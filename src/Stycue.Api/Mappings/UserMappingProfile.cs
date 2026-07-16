using AutoMapper;
using Stycue.Api.DTOs.Users;
using Stycue.Api.Entities;

namespace Stycue.Api.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, CurrentUserResponse>();

            CreateMap<UserProfile, MyUserProfileResponse>()
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<UserProfile, PrivateUserInfoResponse>();
        }
    }
}
