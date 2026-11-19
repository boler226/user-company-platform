using CompanyService.Application.Behaviors;
using CompanyService.Application.Commands.CreateCompany;
using CompanyService.Application.Mappings;
using CompanyService.Application.Validators;
using CompanyService.Infrastructure.DbContext;
using CompanyService.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("PostgreSQLConnection")!,
    builder.Configuration.GetConnectionString("Redis")!, builder.Configuration["RabbitMq:Host"] ?? "localhost");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001); // HTTP
});

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
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
    cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(CreateCompanyValidator).Assembly);

var app = builder.Build();

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
app.Run();
