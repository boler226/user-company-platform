using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Application.Common;

namespace NotificationService.Infrastructure.Persistence;

public sealed class TemplateQueries(MongoDbContext context) : ITemplateQueries
{
    private IMongoCollection<BsonDocument> Collection => context.Database.GetCollection<BsonDocument>("templates");

    private static readonly BsonDocument Projection = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "code", 1 },
        { "channel", 1 },
        { "body", 1 },
        { "version", 1 }
    };

    public Task<TemplateDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        var filter = new BsonDocument { { "_id", ObjectId.Parse(id) }, { "isDeleted", false } };
        return Collection.Find(filter).Project<TemplateDto>(Projection).FirstOrDefaultAsync(ct)!;
    }

    public async Task<PagedResult<TemplateDto>> GetListAsync(int page, int pageSize, CancellationToken ct)
    {
        var filter = new BsonDocument("isDeleted", false);
        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await Collection.Find(filter)
            .Sort(new BsonDocument { { "code", 1 }, { "channel", 1 } })
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<TemplateDto>(Projection)
            .ToListAsync(ct);

        return new PagedResult<TemplateDto>(items, total, page, pageSize);
    }
}
