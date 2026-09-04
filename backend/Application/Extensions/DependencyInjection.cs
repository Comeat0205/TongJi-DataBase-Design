using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IMemberAppService, MemberAppService>();

        // feature/membership-card  会员卡与会籍模块
        services.AddScoped<IMembershipCardAppService, MembershipCardAppService>();
        services.AddScoped<ICardProductAppService, CardProductAppService>();

        // feature/venue-checkin  入场与容量模块
        services.AddScoped<ICheckInOutAppService, CheckInOutAppService>();
        return services;
    }
}


