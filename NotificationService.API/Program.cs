using MediatR;
using NotificationService.API.Grpc;
using Platform.ServiceDefaults.Grpc;
using Platform.ServiceDefaults;
using NotificationService.API;
using NotificationService.Application;
using NotificationService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.ConfigureInternalGrpc();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
    builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://0.0.0.0:5002");
if (builder.Configuration.GetConnectionString("NotificationsDb") is { } connectionString)
    builder.Configuration["MongoDb:ConnectionString"] = connectionString;
builder.Services.AddApplication();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(NotificationCacheInvalidationBehavior<,>));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation();

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<NotificationGrpcService>();
app.Run();
