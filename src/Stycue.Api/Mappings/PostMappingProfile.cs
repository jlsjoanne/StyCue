using AutoMapper;
using Stycue.Api.DTOs.Posts;
using Stycue.Api.Entities;

namespace Stycue.Api.Mappings
{
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            CreateMap<Post, PostDetailResponse>()
                .ForMember(dest => dest.PostId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Author, opt => opt.Ignore())
                  .ForMember(dest => dest.IsOwner, opt => opt.Ignore())
                  .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
                  .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
                  .ForMember(dest => dest.LikeCount, opt => opt.Ignore())
                  .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
                  .ForMember(dest => dest.FavoriteCount, opt => opt.Ignore())
                  .ForMember(dest => dest.IsLiked, opt => opt.Ignore())
                  .ForMember(dest => dest.IsFavorited, opt => opt.Ignore())
                  .ForMember(dest => dest.Images, opt => opt.Ignore())
                  .ForMember(dest => dest.Tags, opt => opt.Ignore());
        }
    }
}
