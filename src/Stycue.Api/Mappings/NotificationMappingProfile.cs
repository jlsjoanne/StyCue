using AutoMapper;
using Stycue.Api.Entities;
using Stycue.Api.DTOs.Notifications;

namespace Stycue.Api.Mappings
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<Notification, NotificationResponse>()
                .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Actor, opt => opt.MapFrom(src => src.ActorUser));
        }
    }
}
