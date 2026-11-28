using Platform.Aggregator.Caching;
using Platform.Grpc.Companies.V1;
using Platform.Grpc.Memberships.V1;
using Platform.Grpc.Notifications.V1;
using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(useHttpResilience: false);
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
    builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddScoped<UserOverviewService>();
builder.Services.AddSingleton<OverviewCache>();
builder.Services.AddTransient<UsersClient>();
builder.Services.AddTransient<CompaniesClient>();
builder.Services.AddTransient<NotificationsClient>();
builder.Services.AddGrpcClient<MembershipReader.MembershipReaderClient>(options =>
    options.Address = new Uri("http://_grpc.users-api"));
builder.Services.AddGrpcClient<CompanyReader.CompanyReaderClient>(options =>
    options.Address = new Uri("http://_grpc.companies-api"));
builder.Services.AddGrpcClient<NotificationReader.NotificationReaderClient>(options =>
    options.Address = new Uri("http://_grpc.notifications-api"));

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
