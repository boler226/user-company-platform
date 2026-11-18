using UsersService.Application.Commands.CreateUser;
using UsersService.Application.Validators;
using FluentValidation;
using UsersService.Application.Mappings;
using UsersService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using UsersService.Infrastructure.DbContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("PostgreSQLConnection")!,
    builder.Configuration["RabbitMq:Host"] ?? "rabbitmq");

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

builder.Services.AddScoped<UserService.Infrastructure.Repositories.MembershipRepository>();
builder.Services.AddHttpClient<
    UserService.Domain.Interfaces.IEntityExistenceChecker,
    UserService.Infrastructure.Services.EntityExistenceChecker>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["CompanyService:Url"] ?? "http://localhost:5001");
    });
builder.Services.AddHttpClient("companies", client => client.BaseAddress = new Uri(builder.Configuration["CompanyService:Url"] ?? "http://localhost:5001"));
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "USER SERVICE V1");
    c.RoutePrefix = "";
});

//app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
