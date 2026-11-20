using Microsoft.Extensions.DependencyInjection;
using UserService.Infrastructure.Memberships;

namespace UserService.Application.Memberships;

public static class MembershipRegistration
{
    public static IServiceCollection AddMemberships(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<IMembershipUnitOfWork>(_ => new MembershipUnitOfWork(connectionString));
        services.AddScoped<IMembershipService, MembershipService>();
        return services;
    }
}
