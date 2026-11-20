using CompanyService.Application.Behaviors;
using CompanyService.Application.Commands.CreateCompany;
using CompanyService.Application.Catalog;
using CompanyService.API.Middleware;
using CompanyService.Application.Mappings;
using CompanyService.Application.Validators;
using CompanyService.Infrastructure.Services;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CompanyExceptionHandler>();
builder.Services.AddScoped<ICompanyService, CompanyCatalogService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("PostgreSQLConnection")!,
    builder.Configuration.GetConnectionString("Redis")!, builder.Configuration["RabbitMq:Host"] ?? "localhost",
    builder.Configuration.GetValue<ushort>("RabbitMq:Port", 5672));

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

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (context, http) =>
        context.Set("TraceId", http.TraceIdentifier);
});
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
app.Run();
