using MongoDB.Bson;
using MongoDB.Driver;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Persistence;

public sealed class NotificationRepository(MongoDbContext context) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(ObjectId id, CancellationToken ct) =>
        context.Notifications.Find(item => item.Id == id && !item.IsDeleted).FirstOrDefaultAsync(ct)!;

    public Task AddAsync(Notification notification, CancellationToken ct) =>
        context.Notifications.InsertOneAsync(notification, cancellationToken: ct);

    public async Task UpdateAsync(Notification notification, long expectedVersion, CancellationToken ct)
    {
        var result = await context.Notifications.ReplaceOneAsync(
            item => item.Id == notification.Id && item.Version == expectedVersion && !item.IsDeleted,
            notification, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException("Notification was changed or deleted by another request.");
    }
}
