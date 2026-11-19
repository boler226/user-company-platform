using DatabaseSetup;

// Administrator credentials are used only to prepare databases and service accounts.
var postgresConnection = Environment.GetEnvironmentVariable("POSTGRES_ADMIN")
    ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=boler2020";
var mongoConnection = Environment.GetEnvironmentVariable("MONGO_ADMIN_URI")
    ?? "mongodb://localhost:27017";
var notificationPassword = Environment.GetEnvironmentVariable("LAB01_NOTIFICATIONS_PASSWORD")
    ?? "Lab01notificationsDemo2026";

await new PostgresDatabaseSetup(postgresConnection).InitializeAsync();
await new MongoDatabaseSetup(mongoConnection, notificationPassword).InitializeAsync();
