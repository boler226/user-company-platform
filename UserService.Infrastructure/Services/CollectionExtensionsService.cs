using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Services;
using UsersService.Domain.Entities;
using UsersService.Domain.Interfaces;
using UsersService.Infrastructure.DbContext;
using UsersService.Infrastructure.Repositories;
using UsersService.Infrastructure.UnitOfWork.Interfaces;

namespace UsersService.Infrastructure.Services;

public static class CollectionExtensionsService
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string rabbitHost = "rabbitmq", ushort rabbitPort = 5672)
    {
        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(connectionString));

        // MassTransit
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, rabbitPort, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
            });
        });

        // Repositories & Services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
