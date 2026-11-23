using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NotificationService.Application.Common;
using NotificationService.Domain.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Infrastructure.Persistence;

public static class MongoMappings
{
    private static readonly Lazy<bool> Registration = new(RegisterMappings);

    public static void Register() => _ = Registration.Value;

    private static bool RegisterMappings()
    {
        var conventions = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("notification-service", conventions,
            type => type.Namespace?.StartsWith("NotificationService.", StringComparison.Ordinal) == true);

        BsonClassMap.RegisterClassMap<BaseEntity>(map => map.AutoMap());
        BsonClassMap.RegisterClassMap<Recipient>(map =>
        {
            map.MapMember(value => value.UserId).SetElementName("userId");
            map.MapMember(value => value.Email).SetElementName("email");
            map.MapMember(value => value.DisplayName).SetElementName("displayName");
            map.MapCreator(value => new Recipient(value.UserId, value.Email, value.DisplayName));
        });
        BsonClassMap.RegisterClassMap<CompanySnapshot>(map =>
        {
            map.MapMember(value => value.Id).SetElementName("id");
            map.MapMember(value => value.Name).SetElementName("name");
            map.MapCreator(value => new CompanySnapshot(value.Id, value.Name));
        });
        BsonClassMap.RegisterClassMap<RecentAttempt>(map =>
        {
            map.MapMember(value => value.AttemptId).SetElementName("attemptId");
            map.MapMember(value => value.Status).SetElementName("status");
            map.MapMember(value => value.OccurredAt).SetElementName("occurredAt");
            map.MapCreator(value => new RecentAttempt(value.AttemptId, value.Status, value.OccurredAt));
        });
        BsonClassMap.RegisterClassMap<Notification>(map =>
        {
            map.AutoMap();
            map.UnmapMember(value => value.RecentAttempts);
            map.UnmapMember(value => value.Payload);
            map.MapField("_recentAttempts").SetElementName("recentAttempts");
            map.MapField("_payload").SetElementName("payload");
            map.MapMember(value => value.Company).SetIgnoreIfNull(true);
        });
        BsonClassMap.RegisterClassMap<NotificationTemplate>(map => map.AutoMap());
        BsonClassMap.RegisterClassMap<CompanySnapshotDto>(map =>
        {
            map.MapMember(value => value.Id).SetElementName("id");
            map.MapMember(value => value.Name).SetElementName("name");
        });

        return true;
    }
}
