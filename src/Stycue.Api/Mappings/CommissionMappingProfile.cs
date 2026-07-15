using AutoMapper;
using Stycue.Api.Entities;
using Stycue.Api.DTOs.Commissions;

namespace Stycue.Api.Mappings
{
    public class CommissionMappingProfile : Profile
    {
        public CommissionMappingProfile()
        {
            CreateMap<Commission, CommissionDetailResponse>()
                  .ForMember(dest => dest.CommissionId, opt => opt.MapFrom(src => src.Id))
                  .ForMember(dest => dest.Author, opt => opt.Ignore())
                  .ForMember(dest => dest.IsOwner, opt => opt.Ignore())
                  .ForMember(dest => dest.CanBoost, opt => opt.Ignore())
                  .ForMember(dest => dest.CanSelectBestComment, opt => opt.Ignore())
                  .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
                  .ForMember(dest => dest.LikeCount, opt => opt.Ignore())
                  .ForMember(dest => dest.FavoriteCount, opt => opt.Ignore())
                  .ForMember(dest => dest.Images, opt => opt.Ignore())
                  .ForMember(dest => dest.Tags, opt => opt.Ignore())
                  .ForMember(dest => dest.Reposts, opt => opt.Ignore());

            CreateMap<CommissionRepost, CommissionRepostResponse>()
                .ForMember(dest => dest.RepostId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Images, opt => opt.Ignore());
        }
    }
}
