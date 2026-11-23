using MongoDB.Bson;

namespace NotificationService.Application.Common;

public static class ValidationRules
{
    public static bool IsObjectId(string? value) =>
        ObjectId.TryParse(value, out var id) && id != ObjectId.Empty;

    public static bool IsUuid(string? value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty;
}
