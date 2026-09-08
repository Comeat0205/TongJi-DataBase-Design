using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class CheckInOutAppService : ICheckInOutAppService
{
    private readonly ICheckInOutRepository _checkInOutRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly ICapacityLogRepository _capLogRepo;
    private readonly ICapacityMovementRepository _capMovementRepo;
    private readonly IUnitOfWork _uow;

    public CheckInOutAppService(
        ICheckInOutRepository checkInOutRepo,
        IVenueRepository venueRepo,
        ICapacityLogRepository capLogRepo,
        ICapacityMovementRepository capMovementRepo,
        IUnitOfWork uow)
    {
        _checkInOutRepo = checkInOutRepo;
        _venueRepo = venueRepo;
        _capLogRepo = capLogRepo;
        _capMovementRepo = capMovementRepo;
        _uow = uow;
    }

    public async Task<CheckInResultDto> CheckInAsync(CheckInRequestDto req, CancellationToken ct = default)
    {
        if (req.CardId <= 0)
            throw new InvalidOperationException("请选择要用于签到的会员卡。");

        // 查卡片 + 扩展表 + 会员
        var card = await _checkInOutRepo.GetCardWithDetailsAsync(req.CardId, ct)
            ?? throw new InvalidOperationException("未找到该会员卡");

        if (req.MemberId is > 0 && card.MemberId != req.MemberId.Value)
            throw new InvalidOperationException("所选会员卡不属于当前会员。");

        // 时效卡：校验在有效期内；次卡：校验有剩余次数。
        if (!card.IsValidNow())
        {
            var type = card.CardType?.Trim();
            if (type == "0")
                throw new InvalidOperationException("次卡次数不足，无法签到。");
            if (type == "1")
                throw new InvalidOperationException("时效卡已过期，无法签到。");
            throw new InvalidOperationException("会员卡当前不可用。");
        }

        var cardType = card.CardType?.Trim() ?? "1"; // 0=次卡, 1=时效卡
        int? remaining = cardType == "0" ? card.CountCardExtension?.RemainingCount : null;
        DateTime? expire = cardType == "1" ? card.TimeCardExtension?.ExpireDate : null;

        // 场馆校验
        var venue = await _venueRepo.GetByIdAsync(req.VenueId, ct);
        if (venue is null)
            throw new InvalidOperationException("场馆不存在");
        if (venue.VenueStatus?.Trim() != "1")
            throw new InvalidOperationException("场馆已关闭");

        var cur = venue.CurrentCapacity ?? 0;
        if (cur >= venue.MaxCapacity)
            throw new InvalidOperationException($"场馆已满 ({cur}/{venue.MaxCapacity})");

        // 同一会员不可重复在场（与所选卡无关）
        var activeByMember = await _checkInOutRepo.GetActiveCheckInByMemberAsync(card.MemberId, ct);
        if (activeByMember is not null)
            throw new InvalidOperationException("该会员已在场内，请先签退。");

        // 写入场记录（关联用户选择的会员卡）
        var nextId = await _checkInOutRepo.GetNextIdAsync(ct);
        var record = new Checkinout
        {
            CheckInOutId = nextId,
            VenueId = req.VenueId,
            CardId = req.CardId,
            CheckInTime = DateTime.Now,
            CheckOutMode = "0"
        };
        await _checkInOutRepo.AddAsync(record, ct);

        // 次卡：扣减 1 次；时效卡：仅校验有效期，不扣次
        if (cardType == "0" && card.CountCardExtension is not null)
        {
            card.CountCardExtension.RemainingCount--;
            remaining = card.CountCardExtension.RemainingCount;
            if (card.CountCardExtension.RemainingCount <= 0)
                card.CardStatus = "0"; // 用完作废
        }

        await _uow.SaveChangesAsync(ct);

        // 触发器已 +1；用 cur+1 记流水，避免再查库。
        await AppendMovementAsync(
            venueId: req.VenueId,
            memberId: card.MemberId,
            eventType: "0",
            eventTime: record.CheckInTime,
            recordedCount: cur + 1,
            maxCapacity: venue.MaxCapacity,
            checkInOutId: record.CheckInOutId,
            ct);

        return new CheckInResultDto
        {
            CheckInOutId = record.CheckInOutId,
            CardId = card.CardId,
            MemberName = card.Member?.Name ?? "",
            VenueName = venue.VenueName,
            CheckInTime = record.CheckInTime,
            CardType = cardType == "0" ? "次卡" : "时效卡",
            CardStatus = card.CardStatus?.Trim() == "1" ? "正常" : "已用完",
            RemainingCount = remaining,
            ExpireDate = expire,
            CapacityWarningLevel = GetCapacityWarningLevel(cur + 1, venue.MaxCapacity)
        };
    }

    public async Task<CheckInOutDto?> CheckOutAsync(int id, CancellationToken ct = default)
    {
        var record = await _checkInOutRepo.GetByIdAsync(id, ct);
        if (record is null) return null;

        if (record.CheckOutTime is not null)
            throw new InvalidOperationException("已退场，勿重复操作");

        var venue = await _venueRepo.GetByIdAsync(record.VenueId, ct)
            ?? throw new InvalidOperationException("场馆不存在");
        var cur = venue.CurrentCapacity ?? 0;
        var after = Math.Max(cur - 1, 0);

        var detailBefore = await _checkInOutRepo.GetWithDetailsAsync(id, ct);
        var memberId = detailBefore?.Card?.MemberId ?? 0;

        record.CheckOutTime = DateTime.Now;
        record.CheckOutMode = "0"; // 手动退场
        await _uow.SaveChangesAsync(ct);

        if (memberId > 0)
        {
            await AppendMovementAsync(
                venueId: record.VenueId,
                memberId: memberId,
                eventType: "1",
                eventTime: record.CheckOutTime.Value,
                recordedCount: after,
                maxCapacity: venue.MaxCapacity,
                checkInOutId: record.CheckInOutId,
                ct);
        }

        var detail = await _checkInOutRepo.GetWithDetailsAsync(id, ct);
        return detail is null ? null : MapDto(detail);
    }

    public async Task<IReadOnlyList<VenueStatusDto>> GetVenueStatusAsync(CancellationToken ct = default)
    {
        var list = await _venueRepo.GetAllAsync(ct);
        return list.Select(v =>
        {
            var cur = v.CurrentCapacity ?? 0;
            return new VenueStatusDto
            {
                VenueId = v.VenueId,
                VenueName = v.VenueName,
                MaxCapacity = v.MaxCapacity,
                CurrentCapacity = cur,
                OccupancyRate = v.MaxCapacity > 0
                    ? Math.Round((decimal)cur / v.MaxCapacity * 100, 1)
                    : 0,
                VenueStatus = v.VenueStatus?.Trim() == "1" ? "营业中" : "已关闭",
                CapacityWarningLevel = GetCapacityWarningLevel(cur, v.MaxCapacity)
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<CheckInOutDto>> GetActiveCheckInsAsync(int venueId, CancellationToken ct = default)
    {
        var list = await _checkInOutRepo.GetActiveCheckInsByVenueAsync(venueId, ct);
        return list.Select(MapDto).ToList();
    }

    public async Task<IReadOnlyList<CheckInOutDto>> GetPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        pageNumber = pageNumber <= 0 ? PagingConstants.DefaultPageNumber : pageNumber;
        pageSize = pageSize <= 0 ? PagingConstants.DefaultPageSize : Math.Min(pageSize, PagingConstants.MaxPageSize);
        var list = await _checkInOutRepo.GetPagedAsync(venueId, pageNumber, pageSize, ct);
        return list.Select(MapDto).ToList();
    }

    public async Task<IReadOnlyList<CapacityLogDto>> GetCapacityLogsPagedAsync(int venueId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        pageNumber = pageNumber <= 0 ? PagingConstants.DefaultPageNumber : pageNumber;
        pageSize = pageSize <= 0 ? PagingConstants.DefaultPageSize : Math.Min(pageSize, PagingConstants.MaxPageSize);
        var list = await _capLogRepo.GetPagedAsync(venueId, pageNumber, pageSize, ct);
        return list.Select(l => new CapacityLogDto
        {
            CapacityLogId = l.CapacityLogId,
            VenueId = l.VenueId,
            VenueName = l.Venue?.VenueName ?? "",
            LogTimestamp = l.LogTimestamp,
            RecordedCapacity = l.RecordedCapacity,
            RecordedCount = l.RecordedCount,
            OccupancyRate = l.OccupancyRate
        }).ToList();
    }

    public async Task<CapacityDailySeriesDto> GetMainVenueDailySeriesAsync(DateOnly date, CancellationToken ct = default)
    {
        var venue = await ResolveMainTrainingVenueAsync(ct)
            ?? throw new InvalidOperationException("未找到主训练馆，请先在场馆管理中维护。");

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var logs = await _capLogRepo.GetByVenueAndDateAsync(venue.VenueId, dayStart, dayEnd, ct);

        return new CapacityDailySeriesDto
        {
            VenueId = venue.VenueId,
            VenueName = venue.VenueName,
            Date = date.ToString("yyyy-MM-dd"),
            MaxCapacity = venue.MaxCapacity,
            Points = logs
                .Where(l => l.LogTimestamp is not null && IsTenMinuteSlot(l.LogTimestamp.Value))
                .Select(l =>
                {
                    var ts = l.LogTimestamp!.Value;
                    return new CapacityLogPointDto
                    {
                        Timestamp = ts,
                        TimeLabel = ts.ToString("HH:mm"),
                        RecordedCount = l.RecordedCount,
                        OccupancyRate = l.OccupancyRate ?? 0m,
                        RecordedCapacity = l.RecordedCapacity
                    };
                })
                .ToList()
        };
    }

    public async Task<IReadOnlyList<CapacityMovementDto>> GetMainVenueMovementsAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        var venue = await ResolveMainTrainingVenueAsync(ct)
            ?? throw new InvalidOperationException("未找到主训练馆，请先在场馆管理中维护。");

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var list = await _capMovementRepo.GetByVenueAndDateAsync(venue.VenueId, dayStart, dayEnd, ct);

        return list.Select(MapMovement).ToList();
    }

    public async Task<CapacityLogDto?> RecordMainVenueSnapshotAsync(CancellationToken ct = default)
    {
        var venue = await ResolveMainTrainingVenueAsync(ct);
        if (venue is null)
        {
            return null;
        }

        var slot = AlignToTenMinutes(DateTime.Now);
        if (await _capLogRepo.ExistsAtAsync(venue.VenueId, slot, ct))
        {
            return null;
        }

        var count = venue.CurrentCapacity ?? 0;
        var max = venue.MaxCapacity;
        var rate = max > 0
            ? Math.Round((decimal)count / max * 100m, 2)
            : 0m;

        var entity = new Capacitylog
        {
            CapacityLogId = await _capLogRepo.GetNextIdAsync(ct),
            VenueId = venue.VenueId,
            LogTimestamp = slot,
            RecordedCapacity = max,
            RecordedCount = count,
            OccupancyRate = rate
        };

        await _capLogRepo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return new CapacityLogDto
        {
            CapacityLogId = entity.CapacityLogId,
            VenueId = venue.VenueId,
            VenueName = venue.VenueName,
            LogTimestamp = entity.LogTimestamp,
            RecordedCapacity = entity.RecordedCapacity,
            RecordedCount = entity.RecordedCount,
            OccupancyRate = entity.OccupancyRate
        };
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var todayCount = await _checkInOutRepo.GetTodayCheckInCountAsync(ct);
        var activeCount = await _checkInOutRepo.GetTotalActiveCountAsync(ct);
        var venues = await GetVenueStatusAsync(ct);

        return new DashboardStatsDto
        {
            TodayCheckIns = todayCount,
            ActiveMembers = activeCount,
            Venues = venues
        };
    }

    public async Task<string> TriggerAutoCheckoutAsync(CancellationToken ct = default)
    {
        return await _checkInOutRepo.ExecuteAutoCheckoutAsync(ct);
    }

    public async Task<CheckInOutDto?> GetMyActiveCheckInAsync(int cardId, CancellationToken ct = default)
    {
        var record = await _checkInOutRepo.GetActiveCheckInByCardAsync(cardId, ct);
        return record is null ? null : MapDto(record);
    }

    public async Task<CheckInOutDto?> GetMyActiveCheckInByMemberAsync(int memberId, CancellationToken ct = default)
    {
        var record = await _checkInOutRepo.GetActiveCheckInByMemberAsync(memberId, ct);
        return record is null ? null : MapDto(record);
    }

    public async Task<MemberCardDto?> GetMemberCardAsync(int cardId, CancellationToken ct = default)
    {
        var card = await _checkInOutRepo.GetCardWithDetailsAsync(cardId, ct);
        if (card is null) return null;

        var isCountCard = card.CardType?.Trim() == "0";
        int? daysToExpire = null;
        if (!isCountCard && card.TimeCardExtension?.ExpireDate != null)
        {
            daysToExpire = (int)(card.TimeCardExtension.ExpireDate.Date - DateTime.Now.Date).TotalDays;
        }

        return new MemberCardDto
        {
            CardId = card.CardId,
            CardType = card.CardType?.Trim() ?? "",
            CardStatus = card.CardStatus?.Trim() ?? "",
            CardTypeName = isCountCard ? "次卡" : "时效卡",
            CardStatusName = card.CardStatus?.Trim() == "1" ? "正常" : "已停用",
            RemainingCount = isCountCard ? card.CountCardExtension?.RemainingCount : null,
            TotalCounts = isCountCard ? card.CountCardExtension?.TotalCounts : null,
            ExpireDate = !isCountCard && card.TimeCardExtension?.ExpireDate != null
                ? card.TimeCardExtension.ExpireDate.ToString("yyyy-MM-dd")
                : null,
            DaysToExpire = daysToExpire,
        };
    }

    static CheckInOutDto MapDto(Checkinout e) => new()
    {
        CheckInOutId = e.CheckInOutId,
        VenueId = e.VenueId,
        VenueName = e.Venue?.VenueName ?? "",
        CardId = e.CardId,
        MemberId = e.Card?.MemberId,
        MemberName = e.Card?.Member?.Name,
        CheckInTime = e.CheckInTime,
        CheckOutTime = e.CheckOutTime,
        CheckOutMode = e.CheckOutMode?.Trim()
    };

    async Task AppendMovementAsync(
        int venueId,
        int memberId,
        string eventType,
        DateTime eventTime,
        int recordedCount,
        int maxCapacity,
        int checkInOutId,
        CancellationToken ct)
    {
        var rate = maxCapacity > 0
            ? Math.Round((decimal)recordedCount / maxCapacity * 100m, 2)
            : 0m;

        var entity = new CapacityMovement
        {
            MovementId = await _capMovementRepo.GetNextIdAsync(ct),
            VenueId = venueId,
            MemberId = memberId,
            EventTime = eventTime,
            EventType = eventType,
            RecordedCount = recordedCount,
            OccupancyRate = rate,
            CheckInOutId = checkInOutId
        };

        await _capMovementRepo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
    }

    static CapacityMovementDto MapMovement(CapacityMovement e)
    {
        var type = e.EventType?.Trim() ?? "0";
        return new CapacityMovementDto
        {
            MovementId = e.MovementId,
            VenueId = e.VenueId,
            VenueName = e.Venue?.VenueName ?? "",
            MemberId = e.MemberId,
            EventTime = e.EventTime,
            EventType = type,
            EventTypeLabel = type == "1" ? "出场" : "进场",
            RecordedCount = e.RecordedCount,
            OccupancyRate = e.OccupancyRate ?? 0m
        };
    }

    /// <summary>
    /// 解析主训练馆：优先名称含「主训练」，否则 VenueId=1，再否则取 ID 最小场馆。
    /// </summary>
    async Task<Venue?> ResolveMainTrainingVenueAsync(CancellationToken ct)
    {
        var venues = await _venueRepo.GetAllAsync(ct);
        if (venues.Count == 0)
        {
            return null;
        }

        return venues.FirstOrDefault(v =>
                   !string.IsNullOrWhiteSpace(v.VenueName)
                   && v.VenueName.Contains("主训练", StringComparison.Ordinal))
               ?? venues.FirstOrDefault(v => v.VenueId == 1)
               ?? venues.OrderBy(v => v.VenueId).FirstOrDefault();
    }

    /// <summary>对齐到整十分钟（秒清零），用于采样时间轴。</summary>
    internal static DateTime AlignToTenMinutes(DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute / 10 * 10, 0);

    /// <summary>仅保留整十分钟采样点，过滤历史自动签退产生的杂乱时间戳。</summary>
    static bool IsTenMinuteSlot(DateTime ts) =>
        ts.Minute % 10 == 0 && ts.Second == 0;

    /// <summary>
    /// 根据当前容量与最大容量计算预警级别（功能点 #7）
    /// </summary>
    static string GetCapacityWarningLevel(int current, int max)
    {
        if (max <= 0) return "normal";
        if (current >= max) return "full";
        var rate = (decimal)current / max;
        return rate >= 0.9m ? "warning" : "normal";
    }
}
