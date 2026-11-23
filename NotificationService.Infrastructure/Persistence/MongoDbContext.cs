using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public bool SeedDemoData { get; set; }
}

public sealed class MongoDbContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<Notification> Notifications => Database.GetCollection<Notification>("notifications");
    public IMongoCollection<NotificationTemplate> Templates => Database.GetCollection<NotificationTemplate>("templates");

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        settings.ClusterConfigurator = cluster => cluster.Subscribe(
            new DiagnosticsActivityEventSubscriber(new InstrumentationOptions { CaptureCommandText = false }));
        var client = new MongoClient(settings);
        Database = client.GetDatabase(options.Value.DatabaseName);
    }
}
