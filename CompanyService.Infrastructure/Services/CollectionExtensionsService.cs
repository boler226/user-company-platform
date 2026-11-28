using CompanyService.Domain.Interfaces;
using CompanyService.Infrastructure.DbContext;
using CompanyService.Infrastructure.Repositories;
using CompanyService.Infrastructure.UnitOfWork.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyService.Infrastructure.Services;

public static class CollectionExtensionsService
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string rabbitHost = "localhost", ushort rabbitPort = 5672)
    {

        // (прибрано Console.WriteLine з рядком підключення: у ньому пароль)

        services.AddDbContext<CompanyDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories & Services (Scoped: живуть рівно один HTTP-запит)
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

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

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
