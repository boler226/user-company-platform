using CompanyService.Application.Caching;
using CompanyService.API.Grpc;
using Platform.ServiceDefaults.Grpc;
using Platform.ServiceDefaults;
using CompanyService.Application.Commands.CreateCompany;
using CompanyService.Application.Catalog;
using CompanyService.API.Middleware;
using CompanyService.Application.Mappings;
using CompanyService.Application.Validators;
using CompanyService.Infrastructure.Services;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.ConfigureInternalGrpc();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
    builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CompanyExceptionHandler>();
builder.Services.AddMemoryCache(options => options.SizeLimit = 1000);
builder.Services.AddSingleton<CompanyReadCache>();
builder.Services.AddScoped<ICompanyService, CompanyCatalogService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("PostgreSQLConnection")!,
    builder.Configuration["RabbitMq:Host"] ?? "localhost",
    builder.Configuration.GetValue<ushort>("RabbitMq:Port", 5672));

builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://0.0.0.0:5001");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-Instance-ID");
    });
});

builder.Services.AddAutoMapper(typeof(CompanyMappingProfile).Assembly);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCompanyCommandHandler).Assembly);
});
builder.Services.AddValidatorsFromAssembly(typeof(CreateCompanyValidator).Assembly);

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();

app.UseExceptionHandler();

var instanceId = Environment.MachineName;
app.Use(async (ctx, next) =>
{
    ctx.Response.OnStarting(() =>
    {
        ctx.Response.Headers["X-Instance-ID"] = instanceId;
        return Task.CompletedTask;
    });
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "COMPANY SERVICE V1");
    c.RoutePrefix = "";
});

//app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<CompanyGrpcService>();
app.Run();
