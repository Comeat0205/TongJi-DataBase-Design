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
        services.AddScoped<IGroupCourseAppService, GroupCourseAppService>();
        services.AddScoped<ICourseTypeAppService, CourseTypeAppService>();
        services.AddScoped<IGroupCourseBookingAppService, GroupCourseBookingAppService>();
        services.AddScoped<IWaitingQueueAppService, WaitingQueueAppService>();
        services.AddScoped<IAbsenceRecordAppService, AbsenceRecordAppService>();
        return services;
    }
}
