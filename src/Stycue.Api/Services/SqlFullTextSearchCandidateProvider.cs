using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Enums;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Services.Models;

namespace Stycue.Api.Services
{
    public class SqlFullTextSearchCandidateProvider : ISearchCandidateProvider
    {
        private readonly AppDbContext _dbContext;

        private const int MaxSearchTerms = 5;
        private const int MaxExpandedTerms = MaxSearchTerms - 1;
        private const int MaxKeywordLength = 100;
        private const int MaxSearchPageSize = 50;

        public SqlFullTextSearchCandidateProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResponse<SearchHit>> FindCandidatesAsync(
            SearchQueryTerms terms,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ValidateTerms(terms);

            var (normalizedPage, normalizedPageSize) = PagingHelper.Normalize(page, pageSize);
            normalizedPageSize = Math.Min(normalizedPageSize, MaxSearchPageSize);

            var containsCondition = BuildContainsCondition(terms);

            var totalCount = await GetTotalCountAsync(containsCondition, cancellationToken);

            var items = totalCount == 0
                ? new List<SearchHit>()
                : await GetPagedHitsAsync(containsCondition, normalizedPage, normalizedPageSize, cancellationToken);

            return new PagedResponse<SearchHit>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalPages = PagingHelper.CalculateTotalPages(totalCount, normalizedPageSize)
            };
        }


        // private helpers

        private static void ValidateTerms(SearchQueryTerms terms)
        {
            ArgumentNullException.ThrowIfNull(terms);

            // 防禦性驗證
            if (string.IsNullOrWhiteSpace(terms.OriginalKeyword) ||
                terms.OriginalKeyword.Length > MaxKeywordLength ||
                !string.Equals(terms.OriginalKeyword, terms.OriginalKeyword.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "OriginalKeyword 必須是已 Trim 且長度不超過 100 的非空白字串。", nameof(terms));
            }

            if (terms.ExpandedKeywords is null || terms.ExpandedKeywords.Count > MaxExpandedTerms)
            {
                throw new ArgumentException(
                    $"ExpandedKeywords 不可為 null，且最多 {MaxExpandedTerms} 個。", nameof(terms));
            }

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                terms.OriginalKeyword
            };

            foreach (var expandedKeyword in terms.ExpandedKeywords)
            {
                if (string.IsNullOrWhiteSpace(expandedKeyword) || expandedKeyword.Length > MaxKeywordLength ||
                    !string.Equals(expandedKeyword, expandedKeyword.Trim(), StringComparison.Ordinal) ||
                    !seenTerms.Add(expandedKeyword))
                {
                    throw new ArgumentException(
                        "ExpandedKeywords 不可包含空白、未 Trim、OriginalKeyword 或忽略大小寫後重複的字詞。",
                        nameof(terms));
                }
            }
        }

        // 讓每個搜尋詞能安全組入 CONTAINSTABLE 的 phrase condition
        private static string EscapeFullTextPhrase(string term)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(term);

            // CONTAINSTABLE 的 phrase 以雙引號界定；
            // 內部雙引號需以兩個雙引號表示。
            var escapedTerm = term.Replace("\"", "\"\"", StringComparison.Ordinal);

            if(escapedTerm.Contains('*', StringComparison.Ordinal))
            {
                throw new ArgumentException("搜尋詞不可包含萬用字元 '*'.", nameof(term));
            }

            return escapedTerm;
        }

        // 將已驗證的原始詞與擴展詞，組成 CONTAINSTABLE 可使用的 condition
        private static string BuildContainsCondition(SearchQueryTerms terms)
        {
            ArgumentNullException.ThrowIfNull(terms);

            var allTerms = new List<string>(1 + terms.ExpandedKeywords.Count)
            {
                terms.OriginalKeyword
            };

            allTerms.AddRange(terms.ExpandedKeywords);

            return string.Join(" OR ", allTerms.Select(term => $"\"{EscapeFullTextPhrase(term)}\""));
        }

        // 取得「符合目前 Full-Text condition，且仍可在前台顯示」的結果總數
        private async Task<int> GetTotalCountAsync(
            string containsCondition, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containsCondition);

            // 計算目前 Full-Text 搜尋條件下，仍可顯示的 SearchDocument 總數
            // 全文索引命中
            // → 對應回 SearchDocument
            // → 排除不可見資料
            // → 算出剩餘筆數
            const string sql = """
                SELECT COUNT_BIG(*) AS [Value]
                FROM CONTAINSTABLE(
                    [dbo].[SearchDocuments],
                    [SearchText], {0}, LANGUAGE 1028) AS [fullTextResult]
                INNER JOIN [dbo].[SearchDocuments] AS [document] ON [document].[Id] = [fullTextResult].[KEY]
                WHERE [document].[IsVisible] = CAST(1 AS bit)
            """;

            var totalCount = await _dbContext.Database
                .SqlQueryRaw<long>(sql, containsCondition)
                .SingleAsync(cancellationToken);

            return checked((int)totalCount);
        }

        

        // 是從 SQL Server Full-Text Search 取得「指定頁面」的候選文件，
        // 並保留資料庫計算出的 RANK 排序，轉成既有的 SearchHit
        private async Task<List<SearchHit>> GetPagedHitsAsync(
            string containsCondition, int page, int pageSize, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(containsCondition);
            ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

            var offset = checked((long)(page - 1) * pageSize);

            const string sql = """
                SELECT [document].[ItemType] AS [ItemType], [document].[ItemId] AS [ItemId], [fullTextResult].[RANK] AS [Rank]
                FROM CONTAINSTABLE([dbo].[SearchDocuments], [SearchText], {0}, LANGUAGE 1028) AS [fullTextResult]
                INNER JOIN [dbo].[SearchDocuments] AS [document] ON [document].[Id] = [fullTextResult].[KEY]
                WHERE [document].[IsVisible] = CAST(1 AS bit)
                ORDER BY [fullTextResult].[RANK] DESC, [document].[UpdatedAt] DESC, [document].[ItemType] ASC, [document].[ItemId] ASC
                OFFSET {1} ROWS
                FETCH NEXT {2} ROWS ONLY
                """;

            var rows = await _dbContext.Database
                .SqlQueryRaw<FullTextSearchHitRow>(sql, containsCondition, offset, pageSize).ToListAsync(cancellationToken);

            return rows
                .Select(row => new SearchHit((HomepageItemType)row.ItemType, row.ItemId, row.Rank)).ToList();
        }
    }
}
