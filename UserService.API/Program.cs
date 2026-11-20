using UsersService.Application.Commands.CreateUser;
using UsersService.Application.Validators;
using FluentValidation;
using UsersService.Application.Mappings;
using UsersService.Infrastructure.Services;
using UserService.Application.Memberships;
using UsersService.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("PostgreSQLConnection")!,
    builder.Configuration["RabbitMq:Host"] ?? "rabbitmq",
    builder.Configuration.GetValue<ushort>("RabbitMq:Port", 5672));

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP 
    //options.ListenAnyIP(5000, listenOptions => listenOptions.UseHttps()); // HTTPS
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommandHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserValidator).Assembly);

builder.Services.AddMemberships(
    builder.Configuration.GetConnectionString("PostgreSQLConnection")
    ?? throw new InvalidOperationException("PostgreSQLConnection is missing."));
builder.Services.AddHttpClient<ICompanySource, CompanySource>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CompanyService:Url"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<
    UserService.Domain.Interfaces.IEntityExistenceChecker,
    UserService.Infrastructure.Services.EntityExistenceChecker>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["CompanyService:Url"] ?? "http://localhost:5001");
    });
builder.Services.AddHttpClient("companies", client => client.BaseAddress = new Uri(builder.Configuration["CompanyService:Url"] ?? "http://localhost:5001"));
var app = builder.Build();
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "USER SERVICE V1");
    c.RoutePrefix = "";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "USER SERVICE V1");
    c.RoutePrefix = "swagger";
});

//app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
