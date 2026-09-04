using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // feature/member-template  会员样板模块
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IMemberAppService, MemberAppService>();

        // feature/membership-card  会员卡与会籍模块
        services.AddScoped<IMembershipCardAppService, MembershipCardAppService>();
        services.AddScoped<ICardProductAppService, CardProductAppService>();

        // feature/venue-checkin  入场与容量模块
        services.AddScoped<ICheckInOutAppService, CheckInOutAppService>();

        // feature/basic-info  基本信息模块
        services.AddScoped<ICoachAppService, CoachAppService>();
        services.AddScoped<IVenueAppService, VenueAppService>();
        services.AddScoped<IEquipmentAppService, EquipmentAppService>();
        
        // feature/personal-training  私教课包与预约模块
        services.AddScoped<IPersonalPackageAppService, PersonalPackageAppService>();
        services.AddScoped<IPtBookingAppService, PtBookingAppService>();
        
        // feature/payment-marketing  支付与营销模块
        services.AddScoped<IPaymentAppService, PaymentAppService>();

        // feature/maintenance   运维与巡检模块
        services.AddScoped<IRepairRecordAppService, RepairRecordAppService>();
        services.AddScoped<IInspectionTaskAppService, InspectionTaskAppService>();
        
        return services;
    }
}
