using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var jwtKey = builder.AddParameter("jwt-key", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("16").WithDataVolume("platform-lab05-postgres");
var usersDb = postgres.AddDatabase("users-db", "lab01_users");
var companiesDb = postgres.AddDatabase("companies-db", "lab03_companies");
var mongo = builder.AddMongoDB("mongo").WithImageTag("8.0").WithDataVolume("platform-lab05-mongo");
var notificationsDb = mongo.AddDatabase("NotificationsDb", "lab04_notifications");
var rabbitUser = builder.AddParameter("rabbit-user", "guest");
var rabbitPassword = builder.AddParameter("rabbit-password", "guest", secret: true);
var rabbit = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPassword);
var redis = builder.AddRedis("redis").WithDataVolume("platform-lab06-redis");
var postgresHost = postgres.GetEndpoint("tcp").Property(EndpointProperty.Host);
var postgresPort = postgres.GetEndpoint("tcp").Property(EndpointProperty.Port);
var mongoHost = mongo.GetEndpoint("tcp").Property(EndpointProperty.Host);
var mongoPort = mongo.GetEndpoint("tcp").Property(EndpointProperty.Port);
var rabbitHost = ReferenceExpression.Create($"{rabbit.GetEndpoint("tcp").Property(EndpointProperty.Host)}");
var rabbitPort = ReferenceExpression.Create($"{rabbit.GetEndpoint("tcp").Property(EndpointProperty.Port)}");

ReferenceExpression ServiceDatabase(string name)
{
    var database = name == "companies" ? "lab03_companies" : "lab01_users";
    return ReferenceExpression.Create(
        $"Host={postgresHost};Port={postgresPort};Database={database};Username=lab01_{name}_app;Password=Lab01{name}Demo2026");
}

var setup = builder.AddProject<Projects.DatabaseSetup>("database-setup")
    .WithEnvironment("POSTGRES_ADMIN", postgres.Resource.ConnectionStringExpression)
    .WithEnvironment("MONGO_ADMIN_URI", mongo.Resource.ConnectionStringExpression)
    .WaitFor(postgres).WaitFor(mongo);

var companies = builder.AddProject<Projects.CompanyService_API>("companies-api", launchProfileName: null)
    .WithHttpEndpoint(name: "http", env: "Rest__Port")
    .WithHttpEndpoint(name: "grpc", env: "Grpc__Port", isProxied: false)
    .WithReference(companiesDb, "PostgreSQLConnection")
    .WithEnvironment("ConnectionStrings__PostgreSQLConnection", ServiceDatabase("companies"))
    .WithReference(redis)
    .WithEnvironment("RabbitMq__Host", rabbitHost)
    .WithEnvironment("RabbitMq__Port", rabbitPort)
    .WaitFor(companiesDb).WaitForCompletion(setup).WaitFor(rabbit).WaitFor(redis)
    .WithHttpHealthCheck("/health", endpointName: "http");

var users = builder.AddProject<Projects.UserService_API>("users-api", launchProfileName: null)
    .WithHttpEndpoint(name: "http", env: "Rest__Port")
    .WithHttpEndpoint(name: "grpc", env: "Grpc__Port", isProxied: false)
    .WithReference(redis).WaitFor(redis)
    .WithReference(usersDb, "PostgreSQLConnection")
    .WithEnvironment("ConnectionStrings__PostgreSQLConnection", ServiceDatabase("users"))
    .WithReference(companies)
    .WithEnvironment("CompanyService__Url", "https+http://companies-api")
    .WithEnvironment("Authentication__Jwt__SecretKey", jwtKey)
    .WithEnvironment("RabbitMq__Host", rabbitHost)
    .WithEnvironment("RabbitMq__Port", rabbitPort)
    .WaitFor(usersDb).WaitForCompletion(setup).WaitFor(rabbit).WaitFor(companies)
    .WithHttpHealthCheck("/health", endpointName: "http");

var notifications = builder.AddProject<Projects.NotificationService_API>("notifications-api", launchProfileName: null)
    .WithHttpEndpoint(name: "http", env: "Rest__Port")
    .WithHttpEndpoint(name: "grpc", env: "Grpc__Port", isProxied: false)
    .WithReference(redis).WaitFor(redis)
    .WithReference(notificationsDb)
    .WithEnvironment("ConnectionStrings__NotificationsDb", ReferenceExpression.Create(
        $"mongodb://lab01_notifications_app:Lab01notificationsDemo2026@{mongoHost}:{mongoPort}/?authSource=lab01_notifications&maxPoolSize=50&serverSelectionTimeoutMS=5000"))
    .WithEnvironment("MongoDb__DatabaseName", "lab04_notifications")
    .WithEnvironment("MongoDb__SeedDemoData", "true")
    .WaitFor(notificationsDb).WaitForCompletion(setup)
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.AddProject<Projects.NotificationService_Worker>("notifications-worker", launchProfileName: null)
    .WithReference(mongo)
    .WithEnvironment("ConnectionStrings__MongoDB", ReferenceExpression.Create(
        $"mongodb://lab01_notifications_app:Lab01notificationsDemo2026@{mongoHost}:{mongoPort}/lab01_notifications?authSource=lab01_notifications"))
    .WithEnvironment("RabbitMq__Host", rabbitHost)
    .WithEnvironment("RabbitMq__Port", rabbitPort)
    .WaitForCompletion(setup).WaitFor(rabbit);

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator", launchProfileName: null)
    .WithHttpEndpoint(name: "http")
    .WithReference(users).WithReference(companies).WithReference(notifications)
    .WaitFor(users).WaitFor(companies).WaitFor(notifications)
    .WithReference(redis).WaitFor(redis)
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.AddProject<Projects.Platform_ApiGateway>("gateway", launchProfileName: null)
    .WithHttpEndpoint(port: 8080, name: "http")
    .WithEnvironment("Authentication__Jwt__SecretKey", jwtKey)
    .WithReference(users).WithReference(companies).WithReference(notifications).WithReference(aggregator)
    .WaitFor(users).WaitFor(companies).WaitFor(notifications).WaitFor(aggregator)
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.Build().Run();
