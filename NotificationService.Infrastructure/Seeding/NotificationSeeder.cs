using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Seeding;

public sealed class NotificationSeeder(MongoDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct)
    {
        var templateId = ObjectId.Parse("690000000000000000000001");
        var secondTemplateId = ObjectId.Parse("690000000000000000000002");
        await InsertMissingAsync(context.Templates,
        [
            new NotificationTemplate("lab04-welcome", "inApp", "Вітаємо на платформі!", templateId),
            new NotificationTemplate("lab04-membership", "email", "Вашу заявку схвалено.", secondTemplateId)
        ], item => item.Id, ct);

        var company = new CompanySnapshot("20000000-0000-0000-0000-000000000001", "Contoso");
        var recipient = new Recipient("10000000-0000-0000-0000-000000000001", "olena@example.test", "Олена Коваль");
        var scheduledAt = new DateTime(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);
        var first = new Notification(templateId, recipient, company, 1, "welcome",
            "Вітаємо на платформі!", scheduledAt, ObjectId.Parse("690000000000000000000011"));
        var attemptId = ObjectId.Parse("690000000000000000000021");
        first.RecordAttempt(new RecentAttempt(attemptId, "Sent", scheduledAt));
        var second = new Notification(secondTemplateId, recipient, company, 3, "membership-approved",
            "Вашу заявку до Contoso схвалено.", scheduledAt, ObjectId.Parse("690000000000000000000012"));
        await InsertMissingAsync(context.Notifications, [first, second], item => item.Id, ct);

        var attempt = new BsonDocument
        {
            { "_id", attemptId },
            { "notificationId", first.Id },
            { "number", 1 },
            { "status", "Sent" },
            { "occurredAt", scheduledAt },
            { "createdBy", "seed" }
        };
        await InsertMissingAsync(context.Database.GetCollection<BsonDocument>("deliveryAttempts"),
            [attempt], item => item["_id"].AsObjectId, ct);
    }

    private static async Task InsertMissingAsync<T>(IMongoCollection<T> collection,
        IReadOnlyCollection<T> documents, Func<T, ObjectId> getId, CancellationToken ct)
    {
        var missing = new List<T>();
        foreach (var document in documents)
        {
            var filter = Builders<T>.Filter.Eq("_id", getId(document));
            if (!await collection.Find(filter).AnyAsync(ct))
                missing.Add(document);
        }

        if (missing.Count == 0)
            return;

        try
        {
            await collection.InsertManyAsync(missing, new InsertManyOptions { IsOrdered = false }, ct);
        }
        catch (MongoBulkWriteException<T> exception) when (
            exception.WriteConcernError is null && exception.WriteErrors.Count > 0
            && exception.WriteErrors.All(error => error.Category == ServerErrorCategory.DuplicateKey))
        {
            foreach (var document in missing)
            {
                if (!await collection.Find(Builders<T>.Filter.Eq("_id", getId(document))).AnyAsync(ct))
                    throw;
            }
        }
    }
}
