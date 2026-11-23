using MongoDB.Bson;
using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(ObjectId id, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
    Task UpdateAsync(Notification notification, long expectedVersion, CancellationToken ct);
}

public interface ITemplateRepository
{
    Task<NotificationTemplate?> GetByIdAsync(ObjectId id, CancellationToken ct);
    Task<bool> ExistsAsync(ObjectId id, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, string channel, CancellationToken ct);
    Task AddAsync(NotificationTemplate template, CancellationToken ct);
    Task UpdateAsync(NotificationTemplate template, long expectedVersion, CancellationToken ct);
}
