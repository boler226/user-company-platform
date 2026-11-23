using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Persistence;

public sealed class TemplateRepository(MongoDbContext context) : ITemplateRepository
{
    public Task<NotificationTemplate?> GetByIdAsync(ObjectId id, CancellationToken ct) =>
        context.Templates.Find(item => item.Id == id && !item.IsDeleted).FirstOrDefaultAsync(ct)!;

    public Task<bool> ExistsAsync(ObjectId id, CancellationToken ct) =>
        context.Templates.Find(item => item.Id == id && !item.IsDeleted).AnyAsync(ct);

    public Task<bool> CodeExistsAsync(string code, string channel, CancellationToken ct) =>
        context.Templates.Find(item => item.Code == code && item.Channel == channel).AnyAsync(ct);

    public Task AddAsync(NotificationTemplate template, CancellationToken ct) =>
        context.Templates.InsertOneAsync(template, cancellationToken: ct);

    public async Task UpdateAsync(NotificationTemplate template, long expectedVersion, CancellationToken ct)
    {
        var result = await context.Templates.ReplaceOneAsync(
            item => item.Id == template.Id && item.Version == expectedVersion && !item.IsDeleted,
            template, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException("Template was changed or deleted by another request.");
    }
}
