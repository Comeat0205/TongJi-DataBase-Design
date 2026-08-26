using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class CheckInOutAppService : ICheckInOutAppService
{
    private readonly ICheckInOutRepository _checkInOutRepo;
    private readonly IVenueRepository _venueRepo;
    private readonly ICapacityLogRepository _capLogRepo;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public CheckInOutAppService(
        ICheckInOutRepository checkInOutRepo,
        IVenueRepository venueRepo,
        ICapacityLogRepository capLogRepo,
        IUnitOfWork uow,
        AppDbContext db)
    {
        _checkInOutRepo = checkInOutRepo;
        _venueRepo = venueRepo;
        _capLogRepo = capLogRepo;
        _uow = uow;
        _db = db;
    }

    public async Task<CheckInResultDto> CheckInAsync(CheckInRequestDto req, CancellationToken ct = default)
    {
        // 查卡片 + 扩展表 + 会员
        var card = await _db.MemberBenefitCards
            .Include(c => c.CountCardExtension)
            .Include(c => c.TimeCardExtension)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.CardId == req.CardId, ct);

        if (card is null)
            throw new InvalidOperationException("未找到该会员卡");

        // 卡片状态 '1' = 正常
        if (card.CardStatus?.Trim() != "1")
            throw new InvalidOperationException("卡片状态异常");

        var cardType = card.CardType?.Trim() ?? "1"; // 0=次卡, 1=时间卡
        int? remaining = null;
        DateTime? expire = null;

        if (cardType == "0")
        {
            // 次卡 - 看剩余次数
            if (card.CountCardExtension is null || card.CountCardExtension.RemainingCount <= 0)
                throw new InvalidOperationException("次卡次数不足");
            remaining = card.CountCardExtension.RemainingCount;
        }
        else
        {
            // 时间卡 - 看有效期
            if (card.TimeCardExtension is null || card.TimeCardExtension.ExpireDate < DateTime.Now.Date)
                throw new InvalidOperationException("时间卡已过期");
            expire = card.TimeCardExtension.ExpireDate;
        }

        // 场馆校验
        var venue = await _venueRepo.GetByIdAsync(req.VenueId, ct);
        if (venue is null)
            throw new InvalidOperationException("场馆不存在");
        if (venue.VenueStatus?.Trim() != "1")
            throw new InvalidOperationException("场馆已关闭");

        var cur = venue.CurrentCapacity ?? 0;
        if (cur >= venue.MaxCapacity)
            throw new InvalidOperationException($"场馆已满 ({cur}/{venue.MaxCapacity})");

        // 防止重复入场
        var dup = await _checkInOutRepo.GetActiveCheckInAsync(req.CardId, req.VenueId, ct);
        if (dup is not null)
            throw new InvalidOperationException("已在场内");

        // 写入场记录
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

        // 次卡扣减
        if (cardType == "0" && card.CountCardExtension is not null)
        {
            card.CountCardExtension.RemainingCount--;
            if (card.CountCardExtension.RemainingCount <= 0)
                card.CardStatus = "0"; // 用完作废
        }

        await _uow.SaveChangesAsync(ct);

        return new CheckInResultDto
        {
            CheckInOutId = record.CheckInOutId,
            MemberName = card.Member?.Name ?? "",
            VenueName = venue.VenueName,
            CheckInTime = record.CheckInTime,
            CardType = cardType == "0" ? "次卡" : "时间卡",
            CardStatus = card.CardStatus?.Trim() == "1" ? "正常" : "已用完",
            RemainingCount = remaining,
            ExpireDate = expire
        };
    }

    public async Task<CheckInOutDto?> CheckOutAsync(int id, CancellationToken ct = default)
    {
        var record = await _checkInOutRepo.GetByIdAsync(id, ct);
        if (record is null) return null;

        if (record.CheckOutTime is not null)
            throw new InvalidOperationException("已退场，勿重复操作");

        record.CheckOutTime = DateTime.Now;
        record.CheckOutMode = "0"; // 手动退场
        await _uow.SaveChangesAsync(ct);

        var detail = await _checkInOutRepo.GetWithDetailsAsync(id, ct);
        return detail is null ? null : MapDto(detail);
    }

    public async Task<IReadOnlyList<VenueStatusDto>> GetVenueStatusAsync(CancellationToken ct = default)
    {
        var list = await _venueRepo.GetAllAsync(ct);
        return list.Select(v => new VenueStatusDto
        {
            VenueId = v.VenueId,
            VenueName = v.VenueName,
            MaxCapacity = v.MaxCapacity,
            CurrentCapacity = v.CurrentCapacity ?? 0,
            OccupancyRate = v.MaxCapacity > 0
                ? Math.Round((decimal)(v.CurrentCapacity ?? 0) / v.MaxCapacity * 100, 1)
                : 0,
            VenueStatus = v.VenueStatus?.Trim() == "1" ? "营业中" : "已关闭"
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
}
