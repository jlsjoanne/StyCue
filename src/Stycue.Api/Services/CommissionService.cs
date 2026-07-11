using AutoMapper;
using Microsoft.Extensions.Options;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Commissions;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Enums;
using Stycue.Api.DTOs.Images;
using Stycue.Api.DTOs.Tags;

namespace Stycue.Api.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITagService _tagService;
        private readonly IPointService _pointService;
        private readonly IImageService _imageService;
        private readonly IImageResponseBuilder _imageResponseBuilder;
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;
        private readonly IMapper _mapper;
        private readonly IOptions<PointsOptions> _pointOptions;

        

        public CommissionService(
            AppDbContext dbContext, ITagService tagService, IPointService pointService, 
            IImageService imageService, IImageResponseBuilder imageResponseBuilder, 
            IUserSummaryResponseBuilder userSummaryResponseBuilder,
            IMapper mapper, IOptions<PointsOptions> pointoptions)
        {
            _dbContext = dbContext;
            _tagService = tagService;
            _pointService = pointService;
            _imageService = imageService;
            _imageResponseBuilder = imageResponseBuilder;
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
            _mapper = mapper;
            _pointOptions = pointoptions;
        }

        // Interface Public Methods


        // private helper methods

        // 委託文是否到期
        private static bool IsExpired(Commission commission, DateTime now)
        {
            return commission.ExpiredAt <= now;
        }

        // 委託文是否可以Repost
        private static bool CanRepost(Commission commission, bool isOwner, bool isExpired)
        {
            return isOwner && isExpired &&
                commission.Status != CommissionStatus.Closed &&
                commission.Status != CommissionStatus.Rewarded &&
                commission.Status != CommissionStatus.NoAward &&
                commission.RepostCount == 0 &&
                commission.RewardSettledAt == null;
        }

        // 委託文是否可以Boost

        private static bool CanBoost(Commission commission, bool isOwner)
        {
            return isOwner &&
                (commission.Status == CommissionStatus.Open || commission.Status == CommissionStatus.Expired) &&
                commission.ClosedAt == null &&
                commission.RewardSettledAt == null &&
                commission.AwardedCommentId == null;
        }

        // 委託文是否可以選最佳留言

        private static bool CanSelectBestComment(Commission commission, bool isOwner)
        {
            return isOwner &&
                commission.AwardedCommentId == null &&
                commission.RewardSettledAt == null &&
                commission.Status != CommissionStatus.Closed &&
                commission.Status != CommissionStatus.Rewarded &&
                commission.Status != CommissionStatus.NoAward &&
                commission.Comments.Any(comment => comment.DeletedAt == null);
        }

        // 查詢委託文詳情
        private async Task<Commission?> FindCommissionForDetailAsync(
            int commissionId, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions.AsNoTracking()
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.Reposts).ThenInclude(r => r.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.Comments)
                .Include(c => c.CommissionLikes)
                .Include(c => c.CommissionFavorites)
                .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);
        }

        // 找到需要更新狀態的委託文
        // 用於Close / Repost / Boost / BestComment / SettleReward
        private async Task<Commission?> FindCommissionForUpdateAsync(
            int commissionId, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions
                .Include(c => c.Comments).ThenInclude(comment => comment.CommentLikes)
                .Include(c => c.Reposts)
                .Include(c => c.CommissionTags)
                .FirstOrDefaultAsync(c => c.Id == commissionId,cancellationToken);
        }

        // 綁定標籤
        private async Task<ApiResponse<T>?> BindCommissionTagsAsync<T>(
            Commission commission, IEnumerable<int> tagIds, CancellationToken cancellationToken)
        {
            try
            {
                var tags = await _tagService.ValidateTagIdsAsync(tagIds, cancellationToken);

                var existingTagIds = commission.CommissionTags
                    .Select(x => x.TagId)
                    .ToHashSet();

                foreach(var tag in tags)
                {
                    if (existingTagIds.Contains(tag.Id))
                    {
                        continue;
                    }
                    commission.CommissionTags.Add(new CommissionTag
                    {
                        Commission = commission,
                        TagId = tag.Id
                    });
                }

                return null;
            }
            catch(InvalidOperationException ex)
            {
                return ApiResponse<T>.FailResult(ex.Message, "INVALID_TAG_IDS");
            }
        }

        // 綁定圖片
        private async Task<ApiResponse<T>?> BindCommissionImagesAsync<T>(
            int userId, IEnumerable<int> imageIds, Commission commission, CancellationToken cancellationToken)
        {
            var imageResult = await _imageService.ValidateBindableImagesAsync(
                userId, imageIds, ImagePurpose.Commission, cancellationToken);

            if( !imageResult.Success || imageResult.Data == null)
            {
                return ApiResponse<T>.FailResult(imageResult.Message, imageResult.ErrorCode);
            }

            foreach(var image in imageResult.Data)
            {
                image.Commission = commission;
            }

            return null;
        }

        // 建立Commission Response圖片
        private IReadOnlyList<ImageResponse> BuildOriginalCommissionImages(Commission commission)
        {
            return _imageResponseBuilder.BuildList(
                commission.Images.Where(i => i.CommissionRepostId == null)
                .OrderBy(i => i.CreatedAt));
        }

        // 建立Commission Response 委託重發內容
        private IReadOnlyList<CommissionRepostResponse> BuildCommissionReposts(Commission commission)
        {
            return commission.Reposts
                .OrderBy(r => r.CreatedAt)
                .Select(repost =>
                {
                    var response = _mapper.Map<CommissionRepostResponse>(repost);

                    response.Images = _imageResponseBuilder.BuildList(repost.Images.OrderBy(image => image.CreatedAt));

                    return response;
                })
                .ToList();
        }

        // 建立回傳委託文詳細內容
        private CommissionDetailResponse BuildCommissionDetailResponse(
            Commission commission, int? currentUserId)
        {
            var now = DateTime.UtcNow;
            var isOwner = OwnershipGuard.IsOwner(commission.UserId, currentUserId);
            var isExpired = IsExpired(commission, now);

            var response = _mapper.Map<CommissionDetailResponse>(commission);

            response.Author = _userSummaryResponseBuilder.Build(commission.User);

            response.IsOwner = isOwner;
            response.IsExpired = isExpired;
            response.CanBoost = CanBoost(commission, isOwner);
            response.CanRepost = CanRepost(commission, isOwner, isExpired);
            response.CanSelectBestComment = CanSelectBestComment(commission, isOwner);

            response.CommentCount = commission.Comments.Count(c => c.DeletedAt == null);
            response.LikeCount = commission.CommissionLikes.Count;
            response.FavoriteCount = commission.CommissionFavorites.Count;

            response.Images = BuildOriginalCommissionImages(commission);
            response.Tags = commission.CommissionTags
                .OrderBy(ct => ct.Tag.Name)
                .Select(ct => _mapper.Map<TagResponse>(ct.Tag))
                .ToList();

            response.Reposts = BuildCommissionReposts(commission);

            return response;
        }
    }
}
