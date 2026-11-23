using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Application.Common;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Seeding;

public sealed class MongoInitializationService(MongoDbContext context, IndexCreationService indexes,
    IDataSeeder seeder, IOptions<MongoDbOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var assembly = typeof(MongoInitializationService).Assembly;
        await using var stream = assembly.GetManifestResourceStream(
            "NotificationService.Infrastructure.Persistence.notification-schemas.json")
            ?? throw new InvalidOperationException("MongoDB collection schemas were not found.");
        using var reader = new StreamReader(stream);
        var schemas = BsonDocument.Parse(await reader.ReadToEndAsync(ct));
        using var cursor = await context.Database.ListCollectionNamesAsync(cancellationToken: ct);
        var existing = (await cursor.ToListAsync(ct)).ToHashSet();

        foreach (var schema in schemas)
        {
            if (existing.Contains(schema.Name))
                continue;

            try
            {
                await context.Database.CreateCollectionAsync(schema.Name,
                    new CreateCollectionOptions<BsonDocument>
                    {
                        Validator = new BsonDocument("$jsonSchema", schema.Value),
                        ValidationLevel = DocumentValidationLevel.Strict,
                        ValidationAction = DocumentValidationAction.Error
                    }, ct);
            }
            catch (MongoCommandException exception) when (exception.Code == 48)
            {
            }
        }

        await indexes.CreateAsync(ct);
        if (options.Value.SeedDemoData)
            await seeder.SeedAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
