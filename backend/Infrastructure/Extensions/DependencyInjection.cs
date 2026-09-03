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

        // 先注册最小可用的会员仓储和工作单元，后续新模块按同样模式扩展。
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICoachRepository, CoachRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}


