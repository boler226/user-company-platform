using NotificationService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.DTOs;
using MassTransit;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Consumers;
using NotificationService.Infrastructure.Reposetories;
using NotificationService.Infrastructure.Services;
using NotificationService.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<MongoDB.Driver.IMongoDatabase>(_ =>
    new MongoDB.Driver.MongoClient(builder.Configuration.GetConnectionString("MongoDB"))
        .GetDatabase("lab01_notifications"));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<CompanyCreateConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
builder.Services.AddScoped<INotificationSender, NotificationSender>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
