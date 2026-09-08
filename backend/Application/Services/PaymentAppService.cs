using Application.DTOs;
using Application.Helpers;
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
    private readonly IPriceListRepository _priceListRepository;
    private readonly IMembershipCardAppService _membershipCardAppService;
    private readonly IPersonalPackageAppService _personalPackageAppService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentAppService(
        IPaymentOrderRepository paymentOrderRepository,
        IVoucherRepository voucherRepository,
        IMemberRepository memberRepository,
        IPriceListRepository priceListRepository,
        IMembershipCardAppService membershipCardAppService,
        IPersonalPackageAppService personalPackageAppService,
        IUnitOfWork unitOfWork)
    {
        _paymentOrderRepository = paymentOrderRepository;
        _voucherRepository = voucherRepository;
        _memberRepository = memberRepository;
        _priceListRepository = priceListRepository;
        _membershipCardAppService = membershipCardAppService;
        _personalPackageAppService = personalPackageAppService;
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
        return orders.Select(o => MapOrder(o, memberId)).ToList();
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

            try
            {
                await EnsureBirthdayVoucherAsync(memberId.Value, cancellationToken);
            }
            catch
            {
                // 生日券补发失败不影响列表。
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

        try
        {
            await EnsureBirthdayVoucherAsync(memberId, cancellationToken);
        }
        catch
        {
            // 生日券补发失败不影响可用券查询。
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
        var issued = 0;

        foreach (var member in members)
        {
            var voucher = await EnsureBirthdayVoucherAsync(member.MemberId, cancellationToken);
            if (voucher is not null)
            {
                issued++;
            }
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

    /// <summary>
    /// 生日福利：依据 MEMBER.BIRTHDAY，在生日当天（至生日后 1 个月内补领窗口）
    /// 自动发放当年生日福利券（¥66，有效期至生日起 1 个月）。每年一张。
    /// </summary>
    private async Task<Voucher?> EnsureBirthdayVoucherAsync(int memberId, CancellationToken cancellationToken)
    {
        if (memberId <= 0)
        {
            return null;
        }

        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member?.Birthday is null)
        {
            return null;
        }

        if (!member.IsActive())
        {
            return null;
        }

        var today = DateTime.Now.Date;
        var year = today.Year;
        var birthdayStart = GetBirthdayInYear(member.Birthday.Value, year);
        var birthdayEnd = birthdayStart.AddMonths(1);

        // 未到今年生日，或已超过 1 个月有效窗口：不发。
        if (today < birthdayStart || today > birthdayEnd)
        {
            return null;
        }

        if (await _voucherRepository.HasBirthdayVoucherForYearAsync(memberId, year, cancellationToken))
        {
            return null;
        }

        var voucher = new Voucher
        {
            VoucherId = await _voucherRepository.GetNextVoucherIdAsync(cancellationToken),
            MemberId = member.MemberId,
            VoucherType = VoucherTypes.Birthday,
            DiscountValue = VoucherTypes.BirthdayAmount,
            ValidUntil = birthdayEnd,
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

        var priceId = request.PriceId
            ?? await _paymentOrderRepository.GetDefaultPriceIdAsync(cancellationToken)
            ?? throw new DomainException("价格表为空，无法创建订单明细。请先维护 PRICE_LIST。");

        if (request.PriceId is not null)
        {
            var price = await _priceListRepository.GetByIdAsync(priceId, cancellationToken)
                ?? throw new DomainException($"未找到编号为 {priceId} 的商品。");
            _ = price;
        }

        // 无 MEMBER_ID 列时，用 BusinessOrderId=MemberId 作为会员归属兜底（购卡/去券后仍能出现在「我的订单」）。
        return await CreatePendingOrderAsync(
            request.MemberId,
            request.TotalAmount,
            priceId,
            businessOrderId: request.MemberId,
            voucherId: request.VoucherId,
            cancellationToken);
    }

    public async Task<PaymentOrderDto> CreateMembershipCardOrderAsync(
        PurchaseMembershipCardRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0)
        {
            throw new DomainException("请提供有效的会员 ID。");
        }

        if (request.PriceId <= 0)
        {
            throw new DomainException("请选择要购买的会员卡商品。");
        }

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {request.MemberId} 的会员。");

        if (!member.IsActive())
        {
            throw new DomainException("当前会员状态不可办卡，请联系前台处理。");
        }

        var price = await _priceListRepository.GetByIdAsync(request.PriceId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {request.PriceId} 的商品。");

        if (!price.ProductType.StartsWith("MEMBERSHIP_", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("该商品不是会员卡类型，无法购买。");
        }

        if (!MembershipCardLabels.IsActiveProductType(price.ProductType))
        {
            throw new DomainException("该商品已下架，无法购买。");
        }

        // 尽量补发新客券，便于自动选券并建立订单-会员关联。
        try
        {
            await EnsureWelcomeVoucherAsync(request.MemberId, cancellationToken);
        }
        catch
        {
            // 补发失败不阻断下单。
        }

        if (price.StandardPrice <= 0)
        {
            throw new DomainException("商品价格无效，无法下单。");
        }

        return await CreatePendingOrderAsync(
            request.MemberId,
            price.StandardPrice,
            request.PriceId,
            businessOrderId: request.MemberId,
            voucherId: request.VoucherId,
            cancellationToken);
    }

    public async Task<PaymentOrderDto> CreatePersonalPackageOrderAsync(
        PurchasePersonalPackageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.MemberId <= 0)
        {
            throw new DomainException("请提供有效的会员 ID。");
        }

        if (request.PriceId <= 0)
        {
            throw new DomainException("请选择要购买的私教课包商品。");
        }

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {request.MemberId} 的会员。");

        if (!member.IsActive())
        {
            throw new DomainException("当前会员状态不可购买课包，请联系前台处理。");
        }

        var price = await _priceListRepository.GetByIdAsync(request.PriceId, cancellationToken)
            ?? throw new DomainException($"未找到编号为 {request.PriceId} 的商品。");

        if (!PersonalPackageProductLabels.IsPersonalPackageProduct(price.ProductType))
        {
            throw new DomainException("该商品不是私教课包类型，无法购买。");
        }

        if (!PersonalPackageProductLabels.IsActiveProductType(price.ProductType))
        {
            throw new DomainException("该商品已下架，无法购买。");
        }

        // 解析并校验课程仍存在（避免下单后支付却无法履约）
        _ = PersonalPackageProductLabels.FromProductType(price.ProductType);

        try
        {
            await EnsureWelcomeVoucherAsync(request.MemberId, cancellationToken);
        }
        catch
        {
            // 补发失败不阻断下单。
        }

        if (price.StandardPrice <= 0)
        {
            throw new DomainException("商品价格无效，无法下单。");
        }

        return await CreatePendingOrderAsync(
            request.MemberId,
            price.StandardPrice,
            request.PriceId,
            businessOrderId: request.MemberId,
            voucherId: request.VoucherId,
            cancellationToken);
    }

    private async Task<PaymentOrderDto> CreatePendingOrderAsync(
        int memberId,
        decimal totalAmount,
        int priceId,
        int businessOrderId,
        int? voucherId,
        CancellationToken cancellationToken)
    {
        var available = await _voucherRepository.GetAvailableAsync(memberId, null, cancellationToken);

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
        var detailId = await _paymentOrderRepository.GetNextDetailIdAsync(cancellationToken);

        var order = new PaymentOrder
        {
            OrderId = orderId,
            BusinessOrderId = businessOrderId,
            TotalAmount = totalAmount,
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
                    TransactionPrice = totalAmount,
                    Quantity = 1,
                    SubtotalAmount = totalAmount
                }
            }
        };

        await _paymentOrderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? throw new DomainException("订单创建失败。");
        return MapOrder(created, memberId);
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

        // 购卡订单：支付成功后按明细商品发卡
        await FulfillMembershipCardsAsync(order, cancellationToken);
        // 购课包订单：支付成功后按明细商品发放私教课包
        await FulfillPersonalPackagesAsync(order, cancellationToken);

        var paid = await _paymentOrderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken)
            ?? order;
        return MapOrder(paid);
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

        if (status == StatusPaid)
        {
            throw new DomainException("已支付订单不可取消。");
        }

        if (status != StatusPending)
        {
            throw new DomainException($"当前订单状态为「{status}」，无法取消。");
        }

        // 待支付取消：券未核销，仅解除占用（VoucherId 可保留作记录）；过期未用券标为作废。
        if (order.Voucher is not null && IsExpired(order.Voucher) && order.Voucher.Status?.Trim() == "0")
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
            RefundAmount = null,
            VoucherRestored = false,
            ActionMessage = "已取消待支付订单。"
        };
    }

    /// <summary>
    /// 支付成功后履约：明细中 MEMBERSHIP_ 商品按条发卡。
    /// </summary>
    private async Task FulfillMembershipCardsAsync(PaymentOrder order, CancellationToken cancellationToken)
    {
        var memberId = order.Voucher?.MemberId ?? order.BusinessOrderId;
        if (memberId <= 0 || order.PaymentDetails is null || order.PaymentDetails.Count == 0)
        {
            return;
        }

        foreach (var detail in order.PaymentDetails)
        {
            var productType = detail.Price?.ProductType;
            if (string.IsNullOrWhiteSpace(productType)
                || !productType.StartsWith("MEMBERSHIP_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var quantity = detail.Quantity <= 0 ? 1 : detail.Quantity;
            for (var i = 0; i < quantity; i++)
            {
                await _membershipCardAppService.CreateAsync(
                    new CreateMembershipCardRequestDto
                    {
                        MemberId = memberId,
                        PriceId = detail.PriceId
                    },
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// 支付成功后履约：明细中 PT_PACKAGE_ 商品按条发放私教课包。
    /// </summary>
    private async Task FulfillPersonalPackagesAsync(PaymentOrder order, CancellationToken cancellationToken)
    {
        var memberId = order.Voucher?.MemberId ?? order.BusinessOrderId;
        if (memberId <= 0 || order.PaymentDetails is null || order.PaymentDetails.Count == 0)
        {
            return;
        }

        foreach (var detail in order.PaymentDetails)
        {
            var productType = detail.Price?.ProductType;
            if (!PersonalPackageProductLabels.IsPersonalPackageProduct(productType))
            {
                continue;
            }

            var quantity = detail.Quantity <= 0 ? 1 : detail.Quantity;
            for (var i = 0; i < quantity; i++)
            {
                await _personalPackageAppService.CreateAsync(
                    new CreatePersonalPackageRequestDto
                    {
                        MemberId = memberId,
                        PriceId = detail.PriceId
                    },
                    cancellationToken);
            }
        }
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

    private static PaymentOrderDto MapOrder(PaymentOrder order, int? fallbackMemberId = null)
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
            // 有券用券归属；新建订单可传入 fallbackMemberId（表无 MEMBER_ID 列）
            MemberId = order.Voucher?.MemberId ?? fallbackMemberId,
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
