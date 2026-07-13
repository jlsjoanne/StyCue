using AutoMapper;
using Stycue.Api.DTOs.Comments;
using Stycue.Api.Entities;

namespace Stycue.Api.Mappings
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CommentResponse>()
                  .ForMember(dest => dest.CommentId,
                      opt => opt.MapFrom(src => src.Id))
                  .ForMember(dest => dest.Author,
                      opt => opt.MapFrom(src => src.User))
                  .ForMember(dest => dest.Images,
                      opt => opt.Ignore())
                  .ForMember(dest => dest.Replies,
                      opt => opt.Ignore())
                  .ForMember(dest => dest.IsOwner,
                      opt => opt.Ignore())
                  .ForMember(dest => dest.CanEdit,
                      opt => opt.Ignore())
                  .ForMember(dest => dest.CanDelete,
                      opt => opt.Ignore())
                  .ForMember(dest => dest.LikeCount,
                      opt => opt.Ignore())
                  .ForMember(dest => dest.IsLiked,
                      opt => opt.Ignore());
        }
    }
}
