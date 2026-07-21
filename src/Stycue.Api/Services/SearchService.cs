using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Search;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Services.Models;

namespace Stycue.Api.Services
{
    public class SearchService : ISearchService
    {
        private readonly AppDbContext _dbContext;
        private readonly IFashionQueryExpander _fashionQueryExpander;
        private readonly ISearchCandidateProvider _searchCandidateProvider;
        private readonly IHomepageItemResponseBuilder _homepageItemResponseBuilder;
        private readonly IFollowService _followService;
        private readonly ILogger<SearchService> _logger;
        private readonly ISearchHistoryService _searchHistoryService;

        public SearchService(
            AppDbContext dbContext, IFashionQueryExpander fashionQueryExpander,
            ISearchCandidateProvider searchCandidateProvider, 
            IHomepageItemResponseBuilder homepageItemResponseBuilder,
            IFollowService followService, ILogger<SearchService> logger, ISearchHistoryService searchHistoryService)
        {
            _dbContext = dbContext;
            _fashionQueryExpander = fashionQueryExpander;
            _searchCandidateProvider = searchCandidateProvider;
            _homepageItemResponseBuilder = homepageItemResponseBuilder;
            _followService = followService;
            _logger = logger;
            _searchHistoryService = searchHistoryService;
        }

        public async Task<ApiResponse<PagedResponse<HomepageItemResponse>>> SearchAsync(
            int? userId, SearchQueryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // input Validation
                if(userId.HasValue && userId.Value <= 0)
                {
                    return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                        "不合法的使用者 ID", "INVALID_USER_ID");
                }

                if(request == null || string.IsNullOrWhiteSpace(request.Keyword))
                {
                    return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                        "請輸入搜尋關鍵字", "VALIDATION_FAILED");
                }

                // keyword trim +  長度驗證(長度不超過100) + 同義詞擴展
                var terms = await _fashionQueryExpander.ExpandAsync(request.Keyword, cancellationToken);

                // 找可見的SearchDocument + Rank + paging
                var candidateResponse = await _searchCandidateProvider.FindCandidatesAsync(
                    terms, request.Page, request.PageSize, cancellationToken);

                // 從candidate中分出post跟commission => 把Id拿出來
                var postIds = candidateResponse.Items
                    .Where(hit => hit.ItemType == HomepageItemType.PostShare || hit.ItemType == HomepageItemType.PostAsk)
                    .Select(hit => hit.ItemId).Distinct().ToList();

                var commissionIds = candidateResponse.Items
                    .Where(hit => hit.ItemType == HomepageItemType.Commission)
                    .Select(hit => hit.ItemId).Distinct().ToList();

                // 用Ids去資料庫把對應的Post/ Commission找出來載入
                var posts = await GetPostsByIdsAsync(postIds, cancellationToken);
                var commissions = await GetCommissionsByIdsAsync(commissionIds, cancellationToken);

                // post/commission => 轉換成dictionary形式 => 接下來可以依hit順序回組HomepageItemResponse
                var postsById = posts.ToDictionary(post => post.Id);
                var commissionsById = commissions.ToDictionary(commission => commission.Id);

                var items = BuildItemsInHitOrder(candidateResponse.Items, postsById, commissionsById, userId);

                // 補追蹤狀態
                await FillAuthorFollowAsync(items, userId, cancellationToken);

                // 組response
                var response = new PagedResponse<HomepageItemResponse>
                {
                    Items = items,
                    Page = candidateResponse.Page,
                    PageSize = candidateResponse.PageSize,
                    TotalCount = candidateResponse.TotalCount,
                    TotalPages = candidateResponse.TotalPages
                };

                // 紀錄搜尋歷史
                if (userId.HasValue)
                {
                    try
                    {
                        await _searchHistoryService.RecordSuccessfulSearchAsync(
                            userId.Value, terms.OriginalKeyword, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch(Exception ex)
                    {
                        _logger.LogWarning(
                            ex, "Search succeeded but failed to record search history. UserId: {UserId}", userId);
                    }
                }

                return ApiResponse<PagedResponse<HomepageItemResponse>>.SuccessResult(
                    response, "搜尋成功");
            }
            catch(ArgumentException ex)
            {
                _logger.LogWarning(ex,
                     "Search validation failed. UserId: {UserId}, Keyword: {Keyword}",
                     userId, request?.Keyword);

                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "搜尋條件不合法", "VALIDATION_FAILED");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Search failed. UserId: {UserId}, Keyword: {Keyword}",
                    userId, request?.Keyword);

                return ApiResponse<PagedResponse<HomepageItemResponse>>.FailResult(
                    "搜尋失敗，請稍後再試", "SEARCH_FAILED");
            }
        }

        // private helpers

        private async Task<List<Post>> GetPostsByIdsAsync(
            IReadOnlyCollection<int> postIds, CancellationToken cancellationToken)
        {
            if(postIds.Count == 0)
            {
                return [];
            }

            return await _dbContext.Posts.AsNoTracking().AsSplitQuery()
                .Where(p => postIds.Contains(p.Id) && p.DeletedAt == null)
                .Include(p => p.User).ThenInclude(u => u.AvatarImage)
                .Include(p => p.Images).ThenInclude(i => i.FashionMetadata)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostLikes).Include(p => p.PostFavorites)
                .Include(p => p.Comments)
                .ToListAsync(cancellationToken);
        }

        private async Task<List<Commission>> GetCommissionsByIdsAsync(
            IReadOnlyCollection<int> commissionIds, CancellationToken cancellationToken)
        {
            if(commissionIds.Count == 0)
            {
                return [];
            }

            return await _dbContext.Commissions.AsNoTracking().AsSplitQuery()
                .Where(c => commissionIds.Contains(c.Id) && c.ClosedAt == null && c.Status != CommissionStatus.Closed)
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.CommissionLikes).Include(c => c.CommissionFavorites)
                .Include(c => c.Comments)
                .ToListAsync(cancellationToken);
        }

        private List<HomepageItemResponse> BuildItemsInHitOrder(
            IReadOnlyList<SearchHit> hits, IReadOnlyDictionary<int, Post> postsByIds,
            IReadOnlyDictionary<int, Commission> commissionByIds, int? currentUserId)
        {
            var items = new List<HomepageItemResponse>();

            foreach(var hit in hits)
            {
                switch (hit.ItemType)
                {
                    case HomepageItemType.PostShare:
                        if( postsByIds.TryGetValue(hit.ItemId, out var sharePost) &&
                            sharePost.PostType == PostType.Share)
                        {
                            items.Add(_homepageItemResponseBuilder.BuildPostItem(sharePost, currentUserId));
                        }
                        break;
                    case HomepageItemType.PostAsk:
                        if( postsByIds.TryGetValue(hit.ItemId, out var askPost) &&
                            askPost.PostType == PostType.Question)
                        {
                            items.Add(_homepageItemResponseBuilder.BuildPostItem(askPost, currentUserId));
                        }
                        break;
                    case HomepageItemType.Commission:
                        if(commissionByIds.TryGetValue(hit.ItemId, out var commission))
                        {
                            items.Add(_homepageItemResponseBuilder.BuildCommissionItem(commission, currentUserId));
                        }
                        break;
                }
            }

            return items;
        }

        private async Task FillAuthorFollowAsync(
            List<HomepageItemResponse> items, int? currentUserId, CancellationToken cancellationToken)
        {
            if( !currentUserId.HasValue || items.Count == 0)
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
    }
}
