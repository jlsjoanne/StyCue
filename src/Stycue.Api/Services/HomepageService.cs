using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Stycue.Api.Services
{
    public class HomepageService : IHomepageService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<HomepageService> _logger;
        private readonly IFollowService _followService;
        private readonly IHomepageItemResponseBuilder _homepageItemResponseBuilder;

        public HomepageService(AppDbContext dbContext,
            ILogger<HomepageService> logger,
            IFollowService followService, IHomepageItemResponseBuilder homepageItemResponseBuilder)
        {
            _dbContext = dbContext;
            _logger = logger;
            _followService = followService;
            _homepageItemResponseBuilder = homepageItemResponseBuilder;
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetHomepageAsync(
            int? userId, HomepageQueryRequest request, CancellationToken cancellationToken)
        {
            request ??= new HomepageQueryRequest();

            try
            {
                
                // validate userId
                if (userId.HasValue && ValidateUserId(userId.Value) is { } userError)
                {
                    return userError;
                }

                // normalize page and pageSize
                var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);

                // validate and normalize sortby and filter
                if(TryParseSortBy(request.SortBy, out var sortBy) is { } sortByError)
                {
                    return sortByError;
                }

                if(TryParseFilter(request.Filter, out var filter) is { } filterError)
                {
                    return filterError;
                }
              
                // 順序: build items by filter, then sort and page
                var response = await BuildHomepageResponseAsync(userId, filter, sortBy, page, pageSize, cancellationToken);

                return ApiResponse<PagedResponse<HomepageItemResponse>>.SuccessResult(
                    response, "首頁列表查詢成功");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Get homepage failed. UserId: {UserId}, SortBy: {SortBy}, Filter: {Filter}, Page: {Page}, PageSize: {PageSize}",
                    userId, request?.SortBy, request?.Filter, request?.Page, request?.PageSize);

                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "取得首頁列表失敗，請稍後再試", "HOMEPAGE_QUERY_FAILED");
            }
        }

        // private helpers

        // validate Id
        private static ApiResponse<PagedResponse<HomepageItemResponse>>? ValidateUserId(int userId)
        {
            return userId > 0 ? null : ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                "不合法的使用者 ID", "INVALID_USER_ID");
        }

        // validate sort by
        private static ApiResponse<PagedResponse<HomepageItemResponse>>? TryParseSortBy(
            string? value, out HomepageSortBy sortBy)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "mostComments" : value.Trim();

            switch (normalized)
            {
                case "latest":
                    sortBy = HomepageSortBy.Latest;
                    return null;
                case "mostLikes":
                case "highestCommissionPoints":
                    sortBy = HomepageSortBy.HighestCommissionPoints;
                    return null;
                case "mostComments":
                    sortBy = HomepageSortBy.MostComments;
                    return null;
                default:
                    sortBy = HomepageSortBy.MostComments;
                    return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                        "不支援的排序方式", "INVALID_SORT_BY");
            }
        }

        // validate filter
        private static ApiResponse<PagedResponse<HomepageItemResponse>>? TryParseFilter(
            string? value, out HomepageFilter filter)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "all" : value.Trim();

            switch (normalized)
            {
                case "all":
                    filter = HomepageFilter.All;
                    return null;
                case "postShare":
                    filter = HomepageFilter.PostShare;
                    return null;
                case "postAsk":
                    filter = HomepageFilter.PostAsk;
                    return null;
                case "commission":
                    filter = HomepageFilter.Commission;
                    return null;
                default:
                    filter = HomepageFilter.All;
                    return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                        "不支援的篩選方式", "INVALID_FILTER");
            }
        }

        // Homepage Item Filter: build item => items to list

        // homepage item to list: BuildCommissionHomepageItemAsync, BuildPostHomepageItemAsync

        private async Task<List<HomepageItemResponse>> BuildCommissionHomepageItemsAsync(
            int? currentUserId, CancellationToken cancellationToken)
        {
            var commissions = await _dbContext.Commissions
                .AsNoTracking().AsSplitQuery()
                .Where(c => c.Status != CommissionStatus.Closed && c.ClosedAt == null)
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.CommissionLikes)
                .Include(c => c.CommissionFavorites)
                .Include(c => c.Comments).ToListAsync(cancellationToken);

            return commissions.Select(commission => _homepageItemResponseBuilder.BuildCommissionItem(commission, currentUserId)).ToList();
        }

        private async Task<List<HomepageItemResponse>> BuildPostHomepageItemsAsync(
            int? currentUserId, PostType postType, CancellationToken cancellationToken)
        {
            var posts = await _dbContext.Posts.AsNoTracking().AsSplitQuery()
                .Where(p => p.DeletedAt == null && p.PostType == postType)
                .Include(p => p.User).ThenInclude(u => u.AvatarImage)
                .Include(p => p.Images).ThenInclude(i => i.FashionMetadata)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes)
                .Include(p => p.PostFavorites)
                .Include(p => p.Comments)
                .ToListAsync(cancellationToken);

            return posts.Select(post => _homepageItemResponseBuilder.BuildPostItem(post, currentUserId)).ToList();
        }

        // Sorting
        private static IEnumerable<HomepageItemResponse> ApplyHomepageSorting(
            IEnumerable<HomepageItemResponse> items, HomepageSortBy sortBy)
        {
            return sortBy switch
            {
                HomepageSortBy.Latest => items.OrderByDescending(item => item.CreatedAt),
                
                HomepageSortBy.HighestCommissionPoints => items
                    .OrderByDescending(item => item.CommissionPoints ?? 0)
                        .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt),

                HomepageSortBy.MostComments => items
                    .OrderByDescending(item => item.CommentCount)
                        .ThenByDescending(item => item.UpdatedAt ?? item.CreatedAt),

                _ => items.OrderByDescending(item => item.CreatedAt)
            };
        }

        // Fill if following into Homepage Response
        private async Task FillAuthorFollowingAsync(
            List<HomepageItemResponse> items, int? currentUserId, CancellationToken cancellationToken)
        {
            if (!currentUserId.HasValue || items.Count == 0)
            {
                return;
            }

            var authorIds = items
                .Select(item => item.Author.UserId)
                .Where(authorId => authorId != currentUserId.Value)
                .Distinct().ToList();

            if(authorIds.Count == 0)
            {
                return;
            }

            var followedAuthorIds = await _followService.GetFollowedUserIdsAsync(
                currentUserId, authorIds, cancellationToken);

            foreach(var item in items)
            {
                item.Author.IsFollowing = item.Author.UserId == currentUserId.Value
                        ? null : followedAuthorIds.Contains(item.Author.UserId);
            }

        }

        // Build Response
        private async Task<PagedResponse<HomepageItemResponse>> BuildHomepageResponseAsync(
            int? currentUserId, HomepageFilter filter, HomepageSortBy sortBy, int page, int pageSize, CancellationToken cancellationToken)
        {
            var items = new List<HomepageItemResponse>();

            var effectiveFilter = sortBy == HomepageSortBy.HighestCommissionPoints
                ? HomepageFilter.Commission : filter;

            if(effectiveFilter is HomepageFilter.All or HomepageFilter.Commission)
            {
                items.AddRange(await BuildCommissionHomepageItemsAsync(currentUserId, cancellationToken));
            }
            if(effectiveFilter is HomepageFilter.All or HomepageFilter.PostShare)
            {
                items.AddRange(await BuildPostHomepageItemsAsync(currentUserId, PostType.Share, cancellationToken));
            }
            if(effectiveFilter is HomepageFilter.All or HomepageFilter.PostAsk)
            {
                items.AddRange(await BuildPostHomepageItemsAsync(currentUserId, PostType.Question, cancellationToken));
            }

            var sortedItems = ApplyHomepageSorting(items, sortBy).ToList();

            var pagedItems = sortedItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            await FillAuthorFollowingAsync(pagedItems, currentUserId, cancellationToken);

            return new PagedResponse<HomepageItemResponse>
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = sortedItems.Count,
                TotalPages = PagingHelper.CalculateTotalPages(sortedItems.Count, pageSize)
            };
        }

    }
}
