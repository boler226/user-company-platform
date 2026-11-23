using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Application.Common;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationQueries(MongoDbContext context) : INotificationQueries
{
    private IMongoCollection<BsonDocument> Collection => context.Database.GetCollection<BsonDocument>("notifications");

    private static readonly BsonDocument Projection = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "templateId", new BsonDocument("$toString", "$templateId") },
        { "recipient", 1 },
        { "company", 1 },
        { "status", 1 },
        { "priority", 1 },
        { "payload", 1 },
        { "scheduledAt", 1 },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", 1 },
        { "recentAttempts", new BsonDocument("$map", new BsonDocument
            {
                { "input", "$recentAttempts" },
                { "as", "attempt" },
                { "in", new BsonDocument
                    {
                        { "attemptId", new BsonDocument("$toString", "$$attempt.attemptId") },
                        { "status", "$$attempt.status" },
                        { "occurredAt", "$$attempt.occurredAt" }
                    }
                }
            })
        }
    };

    public Task<NotificationDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        var filter = new BsonDocument { { "_id", ObjectId.Parse(id) }, { "isDeleted", false } };
        return Collection.Find(filter).Project<NotificationDto>(Projection).FirstOrDefaultAsync(ct)!;
    }

    public async Task<PagedResult<NotificationDto>> GetListAsync(string? userId, string? status,
        string? search, int page, int pageSize, CancellationToken ct)
    {
        var filter = new BsonDocument("isDeleted", false);
        if (userId is not null)
            filter.Add("recipient.userId", userId);
        if (status is not null)
            filter.Add("status", status);
        if (search is not null)
            filter.Add("$text", new BsonDocument("$search", search));

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection.Find(filter)
            .Sort(new BsonDocument { { "createdAt", -1 }, { "_id", 1 } })
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<NotificationDto>(Projection)
            .ToListAsync(ct);

        return new PagedResult<NotificationDto>(items, total, page, pageSize);
    }
}
