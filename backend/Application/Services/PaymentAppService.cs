using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class PaymentAppService : IPaymentAppService
{
    private const string StatusPending = "待支付";
    private const string StatusPaid = "已支付";
    private const string StatusCancelled = "已取消";

    private readonly IPaymentOrderRepository _paymentOrderRepository;
    private readonly IVoucherRepository _voucherRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentAppService(
        IPaymentOrderRepository paymentOrderRepository,
        IVoucherRepository voucherRepository,
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentOrderRepository = paymentOrderRepository;
        _voucherRepository = voucherRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PaymentOrderDto>> GetOrdersAsync(
        int? memberId,
        int? businessOrderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
        var orders = await _paymentOrderRepository.GetListAsync(
            memberId,
            businessOrderId,
            pageNumber,
            pageSize,
            cancellationToken);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<IReadOnlyList<VoucherDto>> GetVouchersAsync(
        int? memberId,
        string? voucherType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (memberId is > 0 && string.IsNullOrWhiteSpace(voucherType))
        {
            try
            {
                await EnsureWelcomeVoucherAsync(memberId.Value, cancellationToken);
            }
            catch
            {
                // 补发失败不影响已有优惠券列表。
            }
        }

        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
        var vouchers = await _voucherRepository.GetListAsync(memberId, voucherType, pageNumber, pageSize, cancellationToken);
        return vouchers.Select(MapVoucher).ToList();
    }

    public async Task<IReadOnlyList<VoucherDto>> GetAvailableVouchersAsync(
        int memberId,
        int? forOrderId,
        CancellationToken cancellationToken = default)
    {
        if (memberId <= 0)
        {
            throw new DomainException("请提供有效的会员 ID。");
        }

        try
        {
            await EnsureWelcomeVoucherAsync(memberId, cancellationToken);
        }
        catch
        {
            // 补发失败不影响可用券查询。
        }

        var vouchers = await _voucherRepository.GetAvailableAsync(memberId, forOrderId, cancellationToken);
        return vouchers.Select(MapVoucher).ToList();
    }

    public async Task<IReadOnlyList<AtRiskMemberDto>> GetAtRiskMembersAsync(
        int inactiveDays,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        inactiveDays = inactiveDays <= 0 ? 30 : inactiveDays;
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

        var rows = await _voucherRepository.GetAtRiskMembersAsync(inactiveDays, pageNumber, pageSize, cancellationToken);
        var today = DateTime.Now.Date;

        return rows.Select(row =>
        {
            var inactive = row.LastCheckInTime is null
                ? inactiveDays
                : Math.Max((today - row.LastCheckInTime.Value.Date).Days, 0);

            return new AtRiskMemberDto
            {
                MemberId = row.Member.MemberId,
                Name = row.Member.Name,
                PhoneNumber = row.Member.PhoneNumber,
                MemberLevel = row.Member.MemberLevel,
                LastCheckInTime = row.LastCheckInTime,
                InactiveDays = inactive,
                UnusedVoucherCount = row.UnusedVoucherCount,
                RiskReason = row.LastCheckInTime is null
                    ? "从未入场"
                    : $"超过 {inactive} 天未入场"
            };
        }).ToList();
    }

    public async Task<VoucherDto> IssueDiscountVoucherAsync(
        IssueDiscountVoucherRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0)
        {
            throw new DomainException("请提供有效的会员 ID。");
        }

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new DomainException("会员不存在。");

        var today = DateTime.Now.Date;
        var voucher = new Voucher
        {
            VoucherId = await _voucherRepository.GetNextVoucherIdAsync(cancellationToken),
            MemberId = member.MemberId,
            VoucherType = VoucherTypes.StaffDiscount,
            DiscountValue = VoucherTypes.StaffDiscountAmount,
            ValidUntil = today.AddDays(7),
            Status = "0"
        };

        await _voucherRepository.AddAsync(voucher, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapVoucher(voucher);
    }

    public async Task<int> IssueDiscountVouchersToAllAsync(CancellationToken cancellationToken = default)
    {
        var members = await _memberRepository.GetActiveMembersAsync(cancellationToken);
        if (members.Count == 0)
        {
            return 0;
        }

        var nextId = await _voucherRepository.GetNextVoucherIdAsync(cancellationToken);
        var today = DateTime.Now.Date;
        var validUntil = today.AddDays(7);

        foreach (var member in members)
        {
            await _voucherRepository.AddAsync(new Voucher
            {
                VoucherId = nextId++,
                MemberId = member.MemberId,
                VoucherType = VoucherTypes.StaffDiscount,
                DiscountValue = VoucherTypes.StaffDiscountAmount,
                ValidUntil = validUntil,
                Status = "0"
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return members.Count;
    }

    public async Task<VoucherDto> IssueWelcomeVoucherAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var voucher = await EnsureWelcomeVoucherAsync(memberId, cancellationToken);
        if (voucher is null)
        {
            throw new DomainException("该会员已领取新客体验券。");
        }

        return MapVoucher(voucher);
    }

    public async Task<int> IssueBirthdayVouchersForTodayAsync(CancellationToken cancellationToken = default)
    {
        var members = await _memberRepository.GetMembersWithBirthdayTodayAsync(cancellationToken);
        var year = DateTime.Now.Year;
        var issued = 0;

        foreach (var member in members)
        {
            if (member.Birthday is null)
            {
                continue;
            }

            if (await _voucherRepository.HasBirthdayVoucherForYearAsync(member.MemberId, year, cancellationToken))
            {
                continue;
            }

            var birthdayStart = GetBirthdayInYear(member.Birthday.Value, year);
            var voucher = new Voucher
            {
                VoucherId = await _voucherRepository.GetNextVoucherIdAsync(cancellationToken),
                MemberId = member.MemberId,
                VoucherType = VoucherTypes.Birthday,
                DiscountValue = VoucherTypes.BirthdayAmount,
                ValidUntil = birthdayStart.AddMonths(1),
                Status = "0"
            };

            await _voucherRepository.AddAsync(voucher, cancellationToken);
            issued++;
        }

        if (issued > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return issued;
    }

    /// <summary>
    /// 注册礼：若该会员尚无新客体验券则自动发放一张。
    /// </summary>
    private async Task<Voucher?> EnsureWelcomeVoucherAsync(int memberId, CancellationToken cancellationToken)
    {
        if (memberId <= 0)
        {
            return null;
        }

        if (await _voucherRepository.HasVoucherAsync(memberId, VoucherTypes.Welcome, cancellationToken))
        {
            return null;
        }

        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var registerDate = (member.RegisterDate ?? DateTime.Now).Date;
        var voucher = new Voucher
        {
            VoucherId = await _voucherRepository.GetNextVoucherIdAsync(cancellationToken),
            MemberId = member.MemberId,
            VoucherType = VoucherTypes.Welcome,
            DiscountValue = VoucherTypes.WelcomeAmount,
            ValidUntil = registerDate.AddYears(1),
            Status = "0"
        };

        await _voucherRepository.AddAsync(voucher, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return voucher;
    }

    public async Task<PaymentOrderDto> CreateOrderAsync(
        CreatePaymentOrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0)
        {
            throw new DomainException("请提供有效的会员 ID。");
        }

        if (request.TotalAmount <= 0)
        {
            throw new DomainException("订单金额必须大于 0。");
        }

        var available = await _voucherRepository.GetAvailableAsync(request.MemberId, null, cancellationToken);
        int? voucherId = request.VoucherId;

        if (voucherId is null)
        {
            // 自动选券：优惠最多；金额相同则优先马上过期。
            voucherId = SelectBestVoucher(available)?.VoucherId;
        }
        else
        {
            EnsureVoucherSelectable(available, voucherId.Value);
        }

        var orderId = await _paymentOrderRepository.GetNextOrderIdAsync(cancellationToken);
        var businessOrderId = await _paymentOrderRepository.GetNextBusinessOrderIdAsync(cancellationToken);
        var detailId = await _paymentOrderRepository.GetNextDetailIdAsync(cancellationToken);
        var priceId = await _paymentOrderRepository.GetDefaultPriceIdAsync(cancellationToken)
            ?? throw new DomainException("价格表为空，无法创建订单明细。请先维护 PRICE_LIST。");

        var order = new PaymentOrder
        {
            OrderId = orderId,
            BusinessOrderId = businessOrderId,
            TotalAmount = request.TotalAmount,
            PaymentStatus = StatusPending,
            CreateTime = DateTime.Now,
            VoucherId = voucherId,
            PaymentDetails =
            {
                new PaymentDetail
                {
                    DetailId = detailId,
                    OrderId = orderId,
                    PriceId = priceId,
                    TransactionPrice = request.TotalAmount,
                    Quantity = 1,
                    SubtotalAmount = request.TotalAmount
                }
            }
        };

        await _paymentOrderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? throw new DomainException("订单创建失败。");
        return MapOrder(created);
    }

    public async Task<PaymentOrderDto?> UpdateOrderVoucherAsync(
        int orderId,
        UpdateOrderVoucherRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var order = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        EnsurePending(order);

        var memberId = order.Voucher?.MemberId
            ?? request.MemberId
            ?? throw new DomainException("请提供会员 ID 后再改券。");

        if (request.VoucherId is null)
        {
            order.VoucherId = null;
            order.Voucher = null;
        }
        else
        {
            var available = await _voucherRepository.GetAvailableAsync(memberId, orderId, cancellationToken);
            EnsureVoucherSelectable(available, request.VoucherId.Value);
            order.VoucherId = request.VoucherId.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        return refreshed is null ? null : MapOrder(refreshed);
    }

    public async Task<PaymentOrderDto?> PayOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var status = order.PaymentStatus?.Trim();
        if (status == StatusPaid)
        {
            throw new DomainException("该订单已支付，无需重复支付。");
        }

        if (status == StatusCancelled)
        {
            throw new DomainException("已取消的订单不能支付。");
        }

        if (!string.IsNullOrEmpty(status) && status != StatusPending)
        {
            throw new DomainException($"当前订单状态为「{status}」，无法支付。");
        }

        if (order.VoucherId is not null)
        {
            var voucher = order.Voucher
                ?? await _voucherRepository.GetByIdTrackedAsync(order.VoucherId.Value, cancellationToken)
                ?? throw new DomainException("关联优惠券不存在。");

            if (IsExpired(voucher))
            {
                throw new DomainException("所选优惠券已过期，请更换优惠券后再支付。");
            }

            if (voucher.Status?.Trim() != "0")
            {
                throw new DomainException("所选优惠券不可用，请更换后再支付。");
            }

            voucher.Status = "1";
            order.Voucher = voucher;
        }

        order.PaymentStatus = StatusPaid;
        order.PaymentFinishTime = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<PaymentOrderDto?> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var status = order.PaymentStatus?.Trim();
        if (status == StatusCancelled)
        {
            throw new DomainException("订单已取消。");
        }

        if (status != StatusPending && status != StatusPaid)
        {
            throw new DomainException($"当前订单状态为「{status}」，无法取消。");
        }

        var wasPaid = status == StatusPaid;
        var refundAmount = wasPaid ? CalcPayable(order) : 0m;

        // 待支付取消：券本来未核销，仅解除占用即可（VoucherId 可保留作记录）。
        // 已支付取消：退实付，不退优惠券（保持已核销）。
        if (!wasPaid && order.Voucher is not null && IsExpired(order.Voucher) && order.Voucher.Status?.Trim() == "0")
        {
            order.Voucher.Status = "2";
        }

        order.PaymentStatus = StatusCancelled;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = MapOrder(order);
        return new PaymentOrderDto
        {
            OrderId = dto.OrderId,
            BusinessOrderId = dto.BusinessOrderId,
            TotalAmount = dto.TotalAmount,
            DiscountValue = dto.DiscountValue,
            PayableAmount = dto.PayableAmount,
            PaymentStatus = dto.PaymentStatus,
            CreateTime = dto.CreateTime,
            PaymentFinishTime = dto.PaymentFinishTime,
            VoucherId = dto.VoucherId,
            VoucherType = dto.VoucherType,
            MemberId = dto.MemberId,
            DetailCount = dto.DetailCount,
            RefundAmount = wasPaid ? refundAmount : null,
            VoucherRestored = false,
            ActionMessage = wasPaid
                ? $"已取消订单，退回实付 {refundAmount:0.00} 元；优惠券不退回。"
                : "已取消待支付订单。"
        };
    }

    private static Voucher? SelectBestVoucher(IReadOnlyList<Voucher> available)
    {
        return available
            .OrderByDescending(v => v.DiscountValue)
            .ThenBy(v => v.ValidUntil)
            .ThenBy(v => v.VoucherId)
            .FirstOrDefault();
    }

    private static void EnsureVoucherSelectable(IReadOnlyList<Voucher> available, int voucherId)
    {
        if (available.All(v => v.VoucherId != voucherId))
        {
            throw new DomainException("所选优惠券不可用（可能已使用、已过期或被其他待支付订单占用）。");
        }
    }

    private static void EnsurePending(PaymentOrder order)
    {
        if (order.PaymentStatus?.Trim() != StatusPending)
        {
            throw new DomainException("仅待支付订单可以修改优惠券。");
        }
    }

    private static bool IsExpired(Voucher voucher) => voucher.ValidUntil.Date < DateTime.Now.Date;

    private static DateTime GetBirthdayInYear(DateTime birthday, int year)
    {
        var day = Math.Min(birthday.Day, DateTime.DaysInMonth(year, birthday.Month));
        return new DateTime(year, birthday.Month, day);
    }

    private static decimal CalcPayable(PaymentOrder order)
    {
        var discount = order.Voucher?.DiscountValue ?? 0m;
        return Math.Max(order.TotalAmount - discount, 0m);
    }

    private static (int pageNumber, int pageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        pageNumber = pageNumber <= 0 ? PagingConstants.DefaultPageNumber : pageNumber;
        pageSize = pageSize <= 0 ? PagingConstants.DefaultPageSize : Math.Min(pageSize, PagingConstants.MaxPageSize);
        return (pageNumber, pageSize);
    }

    private static PaymentOrderDto MapOrder(PaymentOrder order)
    {
        var discount = order.Voucher?.DiscountValue ?? 0m;
        var payable = Math.Max(order.TotalAmount - discount, 0m);

        return new PaymentOrderDto
        {
            OrderId = order.OrderId,
            BusinessOrderId = order.BusinessOrderId,
            TotalAmount = order.TotalAmount,
            DiscountValue = discount,
            PayableAmount = payable,
            PaymentStatus = order.PaymentStatus,
            CreateTime = order.CreateTime,
            PaymentFinishTime = order.PaymentFinishTime,
            VoucherId = order.VoucherId,
            VoucherType = order.Voucher?.VoucherType,
            MemberId = order.Voucher?.MemberId,
            DetailCount = order.PaymentDetails?.Count ?? 0
        };
    }

    private static VoucherDto MapVoucher(Voucher voucher)
    {
        var status = voucher.Status?.Trim();
        var expired = IsExpired(voucher);

        string statusText;
        if (status == "2" || (status == "0" && expired))
        {
            statusText = "过期作废";
            status = "2";
        }
        else
        {
            statusText = status switch
            {
                "0" => "未使用",
                "1" => "已核销",
                _ => string.IsNullOrWhiteSpace(status) ? "未知" : status
            };
        }

        return new VoucherDto
        {
            VoucherId = voucher.VoucherId,
            MemberId = voucher.MemberId,
            VoucherType = voucher.VoucherType,
            DiscountValue = voucher.DiscountValue,
            ValidUntil = voucher.ValidUntil,
            Status = status,
            StatusText = statusText,
            IsExpired = expired || status == "2"
        };
    }
}
