using NotificationService.API.Middleware;

namespace NotificationService.API;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();
        services.AddExceptionHandler<NotificationExceptionHandler>();
        return services;
    }
}
