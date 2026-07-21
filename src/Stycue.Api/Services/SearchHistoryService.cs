using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.SearchHistory;
using Stycue.Api.Entities;
using Stycue.Api.Data;
using Stycue.Api.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Stycue.Api.Services
{
    public class SearchHistoryService : ISearchHistoryService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchHistoryService> _logger;

        private const int DefaultHistoryLimit = 5;
        private const int MaxHistoryLimit = 10;
        private const int MaxKeywordLength = 100;

        public SearchHistoryService(AppDbContext dbContext, IMapper mapper, ILogger<SearchHistoryService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<SearchHistoryResponse>>> GetMyHistoryAsync(
            int userId, SearchHistoryQueryRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                request ??= new SearchHistoryQueryRequest();

                if(ValidateUserId<List<SearchHistoryResponse>>(userId) is { } userError)
                {
                    return userError;
                }

                var limit = NormalizeLimit(request.Limit);

                var histories = await _dbContext.SearchHistories.AsNoTracking()
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.SearchedAt)
                    .ThenByDescending(h => h.Id)
                    .Take(limit).ToListAsync(cancellationToken);

                var response = _mapper.Map<List<SearchHistoryResponse>>(histories);

                return ApiResponse<List<SearchHistoryResponse>>.SuccessResult(response, "取得搜尋紀錄成功");
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Get search history failed. UserId: {UserId}",
                    userId);

                return ApiResponse<List<SearchHistoryResponse>>.FailResult(
                    "取得搜尋紀錄失敗，請稍後再試", "SEARCH_HISTORY_QUERY_FAILED");
            }
        }

        public async Task<ApiResponse<object>> ClearMyHistoryAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ValidateUserId<object>(userId) is { } userError)
                {
                    return userError;
                }

                var deletedCount = await _dbContext.SearchHistories
                    .Where(h => h.UserId == userId).ExecuteDeleteAsync(cancellationToken);

                return ApiResponse<object>.SuccessResult(new { DeletedCount = deletedCount }, "清除搜尋紀錄成功");
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(
                    ex, "Clear search history failed. UserId: {UserId}", userId);

                return ApiResponse<object>.FailResult("清除搜尋紀錄失敗，請稍後再試", "SEARCH_HISTORY_CLEAR_FAILED");
            }
        }

        public async Task RecordSuccessfulSearchAsync(
            int userId, string keyword, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<object>(userId) != null)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), "使用者 ID 必須大於 0");
            }

            ValidateKeyword(keyword);

            var history = await _dbContext.SearchHistories
                .SingleOrDefaultAsync(item => item.UserId == userId && item.Keyword == keyword, cancellationToken);
            var searchedAt = DateTime.UtcNow;

            if(history == null)
            {
                _dbContext.SearchHistories.Add(new SearchHistory
                {
                    UserId = userId,
                    Keyword = keyword,
                    SearchedAt = searchedAt
                });
            }
            else
            {
                history.SearchedAt = searchedAt;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 刪掉多餘的Search History
            await RemoveExcessHistoryAsync(userId, cancellationToken);

            // 若沒有超額紀錄，這次 SaveChangesAsync 不會產生額外 SQL。
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // private helpers

        private static ApiResponse<T>? ValidateUserId<T>(int userId)
        {
            return userId > 0 ? null : ApiResponse<T>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
        }

        private static void ValidateKeyword(string keyword)
        {
            if(string.IsNullOrWhiteSpace(keyword) || keyword.Length > MaxKeywordLength)
            {
                throw new ArgumentException(
                    "搜尋關鍵字必須為長度不超過 100 的非空白字串", nameof(keyword));
            }
        }

        private static int NormalizeLimit(int limit)
        {
            if(limit < 1)
            {
                return DefaultHistoryLimit;
            }

            return Math.Min(limit, MaxHistoryLimit);
        }

        // 每位使用者最多保存 10 筆Search Histories
        private async Task RemoveExcessHistoryAsync(
            int userId, CancellationToken cancellationToken)
        {
            var expiredHistories = await _dbContext.SearchHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.SearchedAt)
                .ThenByDescending(h => h.Id)
                .Skip(MaxHistoryLimit).ToListAsync(cancellationToken);

            if(expiredHistories.Count == 0)
            {
                return;
            }

            _dbContext.SearchHistories.RemoveRange(expiredHistories);
        }
    }
}
