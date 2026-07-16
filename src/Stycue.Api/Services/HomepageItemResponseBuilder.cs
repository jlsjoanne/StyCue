using AutoMapper;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Services
{
    public class HomepageItemResponseBuilder : IHomepageItemResponseBuilder
    {
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;
        private readonly IImageResponseBuilder _imageResponseBuilder;
        private readonly IMapper _mapper;

        public HomepageItemResponseBuilder(
            IUserSummaryResponseBuilder userSummaryResponseBuilder, 
            IImageResponseBuilder imageResponseBuilder, IMapper mapper)
        {
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
            _imageResponseBuilder = imageResponseBuilder;
            _mapper = mapper;
        }

        public HomepageItemResponse BuildPostItem(Post post, int? currentUserId)
        {
            return new HomepageItemResponse
            {
                ItemType = post.PostType == PostType.Share ? HomepageItemType.PostShare : HomepageItemType.PostAsk,
                ItemId = post.Id,
                Author = _userSummaryResponseBuilder.Build(post.User),
                Title = post.Title,
                ContentPreview = BuildContentPreview(post.Content),
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                CommentCount = post.Comments.Count(c => c.DeletedAt == null),
                LikeCount = post.PostLikes.Count,
                IsLiked = currentUserId.HasValue ? post.PostLikes.Any(like => like.UserId == currentUserId.Value) : null,
                FavoriteCount = post.PostFavorites.Count,
                IsFavorited = currentUserId.HasValue ? post.PostFavorites.Any(f => f.UserId == currentUserId.Value) : null,
                Images = _imageResponseBuilder.BuildList(post.Images.Where(i => i.DeletedAt == null).OrderBy(i => i.CreatedAt)),
                Tags = post.PostTags.OrderBy(pt => pt.Tag.Name).Select(pt => _mapper.Map<TagResponse>(pt.Tag)).ToList(),
                PostType = post.PostType,
                CommissionStatus = null,
                CommissionPoints = null,
                ExpiredAt = null
            };
        }

        public HomepageItemResponse BuildCommissionItem(Commission commission, int? currentUserId)
        {
            return new HomepageItemResponse
            {
                ItemType = HomepageItemType.Commission,
                ItemId = commission.Id,
                Author = _userSummaryResponseBuilder.Build(commission.User),
                Title = commission.Title,
                ContentPreview = BuildContentPreview(commission.Content),
                CreatedAt = commission.CreatedAt,
                UpdatedAt = commission.UpdatedAt,
                LikeCount = commission.CommissionLikes.Count,
                IsLiked = currentUserId.HasValue ? commission.CommissionLikes.Any(l => l.UserId == currentUserId.Value) : null,
                FavoriteCount = commission.CommissionFavorites.Count,
                IsFavorited = currentUserId.HasValue ? commission.CommissionFavorites.Any(f => f.UserId == currentUserId.Value) : null,
                CommentCount = commission.Comments.Count(c => c.DeletedAt == null),
                Images = _imageResponseBuilder.BuildList(
                    commission.Images.Where(i => i.DeletedAt == null && i.CommissionRepostId == null)
                    .OrderBy(i => i.CreatedAt)),
                Tags = commission.CommissionTags.OrderBy(ct => ct.Tag.Name)
                    .Select(ct => _mapper.Map<TagResponse>(ct.Tag)).ToList(),
                CommissionStatus = commission.Status,
                CommissionPoints = commission.Points,
                ExpiredAt = commission.ExpiredAt,
                PostType = null
            };
        }

        private const int HomepageContentPreviewMaxLength = 80;
        private static string BuildContentPreview(string? content)
        {
            var normalized = NormalizedPreviewText(content);

            if (normalized.Length <= HomepageContentPreviewMaxLength)
            {
                return normalized;
            }

            return normalized[..HomepageContentPreviewMaxLength];
        }

        private static string NormalizedPreviewText(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            // Join(" ",...)
            // 把切開後的片段用單一半形空白接回來
            // Split((char[]?)null, ...)
            // 用預設 whitespace 字元分割字串。它會把空白、換行、tab 等 whitespace 當成分隔符
            return string.Join(" ", content.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
