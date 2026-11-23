using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Seeding;

public sealed class IndexCreationService(MongoDbContext context)
{
    public async Task CreateAsync(CancellationToken ct)
    {
        var notificationKeys = Builders<Notification>.IndexKeys;
        await context.Notifications.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Notification>(
                notificationKeys.Ascending(item => item.Recipient.UserId)
                    .Descending(item => item.CreatedAt).Ascending(item => item.Id)),
            new CreateIndexModel<Notification>(
                notificationKeys.Descending(item => item.CreatedAt).Ascending(item => item.Id)),
            new CreateIndexModel<Notification>(
                notificationKeys.Ascending(item => item.Status).Ascending(item => item.ScheduledAt)),
            new CreateIndexModel<Notification>(
                notificationKeys.Text("payload.message").Text("recipient.displayName"),
                new CreateIndexOptions { DefaultLanguage = "none" })
        ], ct);

        var templateKeys = Builders<NotificationTemplate>.IndexKeys;
        await context.Templates.Indexes.CreateOneAsync(
            new CreateIndexModel<NotificationTemplate>(
                templateKeys.Ascending(item => item.Code).Ascending(item => item.Channel),
                new CreateIndexOptions { Unique = true }), cancellationToken: ct);

        var attempts = context.Database.GetCollection<BsonDocument>("deliveryAttempts");
        await attempts.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { { "notificationId", 1 }, { "number", 1 } },
                new CreateIndexOptions { Unique = true }), cancellationToken: ct);
    }
}
