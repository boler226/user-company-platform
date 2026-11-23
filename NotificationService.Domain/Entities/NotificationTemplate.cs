using NotificationService.Domain.Common;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Domain.Entities;

public sealed class NotificationTemplate : BaseEntity
{
    public string Code { get; private set; } = "";
    public string Channel { get; private set; } = "";
    public string Body { get; private set; } = "";

    private NotificationTemplate()
    {
    }

    public NotificationTemplate(string code, string channel, string body, MongoDB.Bson.ObjectId? id = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100)
            throw new DomainException("Template code must contain 1 to 100 characters.");
        if (channel is not ("email" or "inApp"))
            throw new DomainException("Channel must be email or inApp.");

        Code = code.Trim().ToLowerInvariant();
        Channel = channel;
        SetBody(body);
    }

    public void Update(string body)
    {
        if (IsDeleted)
            throw new ConflictException("Deleted templates cannot be edited.");

        SetBody(body);
        Touch();
    }

    private void SetBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > 4000)
            throw new DomainException("Template body must contain 1 to 4000 characters.");

        Body = body.Trim();
    }
}
