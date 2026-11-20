using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithImageTag("16").WithDataVolume();
var mongo = builder.AddMongoDB("mongo").WithImageTag("8.0").WithDataVolume();
var rabbitUser = builder.AddParameter("rabbit-user", "guest");
var rabbitPassword = builder.AddParameter("rabbit-password", "guest", secret: true);
var rabbit = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPassword);
var redis = builder.AddRedis("redis");

var postgresHost = postgres.GetEndpoint("tcp").Property(EndpointProperty.Host);
var postgresPort = postgres.GetEndpoint("tcp").Property(EndpointProperty.Port);
var mongoHost = mongo.GetEndpoint("tcp").Property(EndpointProperty.Host);
var mongoPort = mongo.GetEndpoint("tcp").Property(EndpointProperty.Port);
var rabbitHost = ReferenceExpression.Create($"{rabbit.GetEndpoint("tcp").Property(EndpointProperty.Host)}");
var rabbitPort = ReferenceExpression.Create($"{rabbit.GetEndpoint("tcp").Property(EndpointProperty.Port)}");

var setup = builder.AddProject<Projects.DatabaseSetup>("database-setup")
    .WithEnvironment("POSTGRES_ADMIN", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("MONGO_ADMIN_URI", mongo.Resource.ConnectionStringExpression)
    .WaitFor(postgres)
    .WaitFor(mongo);

ReferenceExpression ServiceDatabase(string context)
{
    var database = context == "companies" ? "lab03_companies" : "lab01_users";
    return ReferenceExpression.Create(
        $"Host={postgresHost};Port={postgresPort};Database={database};Username=lab01_{context}_app;Password=Lab01{context}Demo2026");
}

var companies = builder.AddProject<Projects.CompanyService_API>("companies-api", launchProfileName: null)
    .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "http", isProxied: false)
    .WithEnvironment("ConnectionStrings__PostgreSQLConnection", ServiceDatabase("companies"))
    .WithReference(redis, "Redis")
    .WithEnvironment("RabbitMq__Host", rabbitHost)
    .WithEnvironment("RabbitMq__Port", rabbitPort)
    .WaitForCompletion(setup)
    .WaitFor(rabbit)
    .WaitFor(redis);

builder.AddProject<Projects.UserService_API>("users-api", launchProfileName: null)
    .WithHttpEndpoint(port: 5000, targetPort: 5000, name: "http", isProxied: false)
    .WithEnvironment("ConnectionStrings__PostgreSQLConnection", ServiceDatabase("users"))
    .WithEnvironment("CompanyService__Url", companies.GetEndpoint("http"))
    .WithEnvironment("RabbitMq__Host", rabbitHost)
    .WithEnvironment("RabbitMq__Port", rabbitPort)
    .WaitForCompletion(setup)
    .WaitFor(rabbit)
    .WaitFor(companies);

builder.AddProject<Projects.NotificationService_Worker>("notifications-worker", launchProfileName: null)
    .WithEnvironment("ConnectionStrings__MongoDB", ReferenceExpression.Create(
        $"mongodb://lab01_notifications_app:Lab01notificationsDemo2026@{mongoHost}:{mongoPort}/lab01_notifications?authSource=lab01_notifications"))
    .WithEnvironment("RabbitMq__Host", rabbitHost)
    .WithEnvironment("RabbitMq__Port", rabbitPort)
    .WaitForCompletion(setup)
    .WaitFor(rabbit);

builder.Build().Run();
