using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddScoped<UserOverviewService>();
builder.Services.AddHttpClient<UsersClient>(client => client.BaseAddress = new Uri("https+http://users-api"));
builder.Services.AddHttpClient<CompaniesClient>(client => client.BaseAddress = new Uri("https+http://companies-api"));
builder.Services.AddHttpClient<NotificationsClient>(client => client.BaseAddress = new Uri("https+http://notifications-api"));

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
