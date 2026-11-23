using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Common;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Seeding;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();
        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection("MongoDb"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "MongoDB connection string is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "MongoDB database name is required.")
            .Validate(options => options.DatabaseName != "lab01_notifications",
                "The lab04 service requires an ObjectId database. Do not point it at the legacy lab01 database.")
            .ValidateOnStart();
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<INotificationQueries, NotificationQueries>();
        services.AddScoped<ITemplateQueries, TemplateQueries>();
        services.AddSingleton<IDataSeeder, NotificationSeeder>();
        services.AddSingleton<IndexCreationService>();
        services.AddHostedService<MongoInitializationService>();
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");
        return services;
    }
}
