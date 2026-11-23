using AutoMapper;
using MongoDB.Bson;
using NotificationService.Domain.Entities;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Application.Common;

public sealed class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<ObjectId, string>().ConvertUsing(id => id.ToString());
        CreateMap<Recipient, RecipientDto>();
        CreateMap<CompanySnapshot, CompanySnapshotDto>();
        CreateMap<RecentAttempt, RecentAttemptDto>();
        CreateMap<Notification, NotificationDto>()
            .ForMember(destination => destination.Payload,
                options => options.MapFrom(source => source.Payload.ToDictionary(item => item.Key, item => item.Value)));
        CreateMap<NotificationTemplate, TemplateDto>();
    }
}
