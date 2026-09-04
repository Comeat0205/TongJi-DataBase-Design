using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 数据库连接只在基础设施层读取和装配，上层不直接依赖连接细节。
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("未配置 DefaultConnection 数据库连接字符串。");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseOracle(connectionString);
        });

        // feature/member-template  会员样板模块
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        
        // feature/membership-card  会员卡与会籍模块
        services.AddScoped<IMembershipCardRepository, MembershipCardRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        
        // feature/venue-checkin  入场与容量模块
        services.AddScoped<ICheckInOutRepository, CheckInOutRepository>();
        services.AddScoped<ICapacityLogRepository, CapacityLogRepository>();

        // feature/basic-info  基本信息模块
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICoachRepository, CoachRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        
        // feature/personal-training  私教课包与预约模块
        services.AddScoped<IPersonalPackageRepository, PersonalPackageRepository>();
        services.AddScoped<IPtBookingRepository, PtBookingRepository>();
        
        
        // feature/payment-marketing  支付与营销模块
        services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        
        // feature/maintenance   运维与巡检模块
        services.AddScoped<IRepairRecordRepository, RepairRecordRepository>();
        services.AddScoped<IInspectionTaskRepository, InspectionTaskRepository>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
