using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.Enums;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Services.Models;
using Stycue.Api.Entities;

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

        // Build Homepage Candidate
        private IQueryable<HomepageCandidate> BuildCommissionHomepageCandidatesQuery()
        {
            return _dbContext.Commissions.AsNoTracking()
                .Where(c => c.Status != CommissionStatus.Closed && c.ClosedAt == null)
                .Select(c => new HomepageCandidate(
                    HomepageItemType.Commission, c.Id, c.CreatedAt, c.UpdatedAt ?? c.CreatedAt,
                    c.Comments.Count(c => c.DeletedAt == null), c.Points));
        }

        private IQueryable<HomepageCandidate> BuildPostHomepageCandidatesQuery(PostType postType)
        {
            return _dbContext.Posts.AsNoTracking()
                .Where(p => p.DeletedAt == null && p.PostType == postType)
                .Select(p => new HomepageCandidate(postType == PostType.Share ? HomepageItemType.PostShare : HomepageItemType.PostAsk,
                p.Id, p.CreatedAt, p.UpdatedAt ?? p.CreatedAt,
                p.Comments.Count(c => c.DeletedAt == null), null));
        }

        // sort candidates
        private IQueryable<HomepageCandidate> ApplyCandidateSorting(IQueryable<HomepageCandidate> candidates, HomepageSortBy sortBy)
        {
            switch (sortBy)
            {
                case HomepageSortBy.MostComments:
                    return candidates
                        .OrderByDescending(c => c.CommentCount)
                        .ThenByDescending(c => c.EffectiveUpdatedAt)
                        .ThenBy(c => c.ItemType).ThenByDescending(c => c.ItemId);
                case HomepageSortBy.Latest:
                    return candidates.OrderByDescending(c => c.CreatedAt)
                        .ThenBy(c => c.ItemType).ThenByDescending(c => c.ItemId);
                case HomepageSortBy.HighestCommissionPoints:
                    return candidates.OrderByDescending(c => c.CommissionPoints ?? 0)
                        .ThenByDescending(c => c.EffectiveUpdatedAt)
                        .ThenBy(c => c.ItemType).ThenByDescending(c => c.ItemId);
                default:
                    throw new InvalidOperationException($"Unsupported homepage sortby: {sortBy}");
            }
        }

        // Get post and commission detail

        private Task<List<Post>> GetPostDetailsAsync(IReadOnlyCollection<int> postIds, CancellationToken cancellationToken)
        {
            if(postIds.Count == 0)
            {
                return Task.FromResult(new List<Post>());
            }

            return _dbContext.Posts.AsNoTracking().AsSplitQuery()
                .Where(p => postIds.Contains(p.Id) && p.DeletedAt == null)
                .Include(p => p.User).ThenInclude(u => u.AvatarImage)
                .Include(p => p.Images).ThenInclude(i => i.FashionMetadata)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes)
                .Include(p => p.PostFavorites)
                .Include(p => p.Comments).ToListAsync(cancellationToken);
        }

        private Task<List<Commission>> GetCommissionDetailsAsync(IReadOnlyCollection<int> commissionIds, CancellationToken cancellationToken)
        {
            if(commissionIds.Count == 0)
            {
                return Task.FromResult(new List<Commission>());
            }

            return _dbContext.Commissions.AsNoTracking().AsSplitQuery()
                .Where(c => commissionIds.Contains(c.Id) && c.ClosedAt == null && c.Status != CommissionStatus.Closed)
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.CommissionLikes).Include(c => c.CommissionFavorites)
                .Include(c => c.Comments).ToListAsync(cancellationToken);
        }

        // build paged items
        private List<HomepageItemResponse> BuildPagedItems(
            IReadOnlyList<HomepageCandidate> pagedCandidates,
            IReadOnlyDictionary<int, Post> postsById, IReadOnlyDictionary<int, Commission> commissionsById,
            int? currentUserId)
        {
            var items = new List<HomepageItemResponse>(pagedCandidates.Count);

            foreach(var candidate in pagedCandidates)
            {
                switch (candidate.ItemType)
                {
                    case HomepageItemType.PostShare:
                    case HomepageItemType.PostAsk:
                        if(postsById.TryGetValue(candidate.ItemId, out var post))
                        {
                            items.Add(_homepageItemResponseBuilder.BuildPostItem(post, currentUserId));
                        }
                        break;
                    case HomepageItemType.Commission:
                        if(commissionsById.TryGetValue(candidate.ItemId, out var commission))
                        {
                            items.Add(_homepageItemResponseBuilder.BuildCommissionItem(commission, currentUserId));
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported homepage item type: {candidate.ItemType}");
                }
            }

            return items;
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
            IQueryable<HomepageCandidate> candidates;

            var effectiveFilter = sortBy == HomepageSortBy.HighestCommissionPoints ? HomepageFilter.Commission : filter;

            switch (effectiveFilter)
            {
                case HomepageFilter.PostShare:
                    candidates = BuildPostHomepageCandidatesQuery(PostType.Share);
                    break;
                case HomepageFilter.PostAsk:
                    candidates = BuildPostHomepageCandidatesQuery(PostType.Question);
                    break;
                case HomepageFilter.Commission:
                    candidates = BuildCommissionHomepageCandidatesQuery();
                    break;
                case HomepageFilter.All:
                    var sharePosts = BuildPostHomepageCandidatesQuery(PostType.Share);
                    var askPosts = BuildPostHomepageCandidatesQuery(PostType.Question);
                    var commissionCandidates = BuildCommissionHomepageCandidatesQuery();

                    candidates = sharePosts.Concat(askPosts).Concat(commissionCandidates);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported homepage filter: {effectiveFilter}");
            }

            var sortedCandidates = ApplyCandidateSorting(candidates, sortBy);

            var totalCount = await sortedCandidates.CountAsync(cancellationToken);

            var pageCandidates = await sortedCandidates
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            var postIds = pageCandidates.Where(c => c.ItemType == HomepageItemType.PostShare || c.ItemType == HomepageItemType.PostAsk)
                .Select(c => c.ItemId).Distinct().ToArray();
            var commissionIds = pageCandidates.Where(c => c.ItemType == HomepageItemType.Commission)
                .Select(c => c.ItemId).Distinct().ToArray();

            var posts = await GetPostDetailsAsync(postIds, cancellationToken);
            var commissions = await GetCommissionDetailsAsync(commissionIds, cancellationToken);

            var postsById = posts.ToDictionary(post => post.Id);
            var commissionsById = commissions.ToDictionary(commission => commission.Id);

            var pagedItems = BuildPagedItems(pageCandidates,
                postsById, commissionsById, currentUserId);

            await FillAuthorFollowingAsync(pagedItems, currentUserId, cancellationToken);

            return new PagedResponse<HomepageItemResponse>
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = PagingHelper.CalculateTotalPages(totalCount, pageSize)
            };
        }
    }
}
