using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Reposetories;

public class UserNotificationRepository(IMongoDatabase database) : IUserNotificationRepository
{
    private IMongoCollection<BsonDocument> Notifications =>
        database.GetCollection<BsonDocument>("notifications");

    public async Task AddAsync(UserNotificationSchedule schedule)
    {
        var recipient = new BsonDocument
        {
            { "userId", schedule.UserId.ToString() },
            { "email", schedule.Email },
            { "displayName", schedule.Email }
        };

        var document = new BsonDocument
        {
            { "_id", schedule.Id.ToString() },
            { "templateId", "registration-email" },
            { "recipient", recipient },
            { "status", "Pending" },
            { "priority", 1 },
            { "payload", new BsonDocument("kind", "registration") },
            { "recentAttempts", new BsonArray() },
            { "createdAt", schedule.RegisteredAt },
            { "createdBy", "UserRegistered" },
            { "updatedAt", DateTime.UtcNow },
            { "updatedBy", "UserRegistered" },
            { "isDeleted", false },
            { "version", 1 },
            { "scheduledAt", schedule.RegisteredAt.AddDays(2) }
        };

        await Notifications.UpdateOneAsync(
            new BsonDocument("_id", schedule.Id.ToString()),
            new BsonDocument("$setOnInsert", document),
            new UpdateOptions { IsUpsert = true });
    }

    public async Task<List<UserNotificationSchedule>> GetUsersToNotifyAsync()
    {
        var filter = new BsonDocument
        {
            { "status", "Pending" },
            { "isDeleted", false },
            { "payload.kind", "registration" },
            { "scheduledAt", new BsonDocument("$lte", DateTime.UtcNow) }
        };

        var documents = await Notifications.Find(filter).ToListAsync();

        return documents.Select(document => new UserNotificationSchedule
        {
            Id = Guid.Parse(document["_id"].AsString),
            UserId = Guid.Parse(document["recipient"]["userId"].AsString),
            Email = document["recipient"]["email"].AsString,
            RegisteredAt = document["createdAt"].ToUniversalTime()
        }).ToList();
    }

    public async Task MarkAsNotifiedAsync(Guid id)
    {
        var notificationId = id.ToString();
        var attemptId = notificationId + "-sent";
        var attempts = database.GetCollection<BsonDocument>("deliveryAttempts");
        var attempt = new BsonDocument
        {
            { "_id", attemptId },
            { "notificationId", notificationId },
            { "number", 1 },
            { "status", "Sent" },
            { "occurredAt", DateTime.UtcNow },
            { "createdBy", "worker" }
        };

        await attempts.UpdateOneAsync(
            new BsonDocument("_id", attemptId),
            new BsonDocument("$setOnInsert", attempt),
            new UpdateOptions { IsUpsert = true });

        // A retry after a partial failure must reuse the original attempt timestamp.
        attempt = await attempts.Find(new BsonDocument("_id", attemptId)).SingleAsync();

        var recentAttempt = new BsonDocument
        {
            { "attemptId", attemptId },
            { "status", "Sent" },
            { "occurredAt", attempt["occurredAt"] }
        };

        var filter = new BsonDocument
        {
            { "_id", notificationId },
            { "status", "Pending" }
        };

        var updatedFields = new BsonDocument
        {
            { "status", "Sent" },
            { "updatedBy", "worker" },
            { "recentAttempts", new BsonArray { recentAttempt } }
        };

        var update = new BsonDocument
        {
            { "$set", updatedFields },
            { "$currentDate", new BsonDocument("updatedAt", true) },
            { "$inc", new BsonDocument("version", 1) }
        };

        await Notifications.UpdateOneAsync(filter, update);
    }
}
