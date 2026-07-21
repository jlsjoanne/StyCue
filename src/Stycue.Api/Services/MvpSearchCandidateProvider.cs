using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Services.Models;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Stycue.Api.Services
{
    public class MvpSearchCandidateProvider : ISearchCandidateProvider
    {
        private readonly AppDbContext _dbContext;

        private const int MaxCandidateDocuments = 500;
        private const int MaxKeywordLength = 100;
        private const int MaxSearchTerms = 5;
        private const int MaxExpandedTerms = MaxSearchTerms - 1;
        private const int MaxSearchPageSize = 50;

        public MvpSearchCandidateProvider(AppDbContext dbContext)
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

            // 目前共用 PagingHelper 上限為 100；搜尋規格需限制為 50。
            normalizedPageSize = Math.Min(normalizedPageSize, MaxSearchPageSize);

            var candidateDocuments = await _dbContext.SearchDocuments.AsNoTracking()
                .Where(document => document.IsVisible)
                .OrderByDescending(document => document.UpdatedAt)
                .Take(MaxCandidateDocuments)
                .Select(document => new SearchDocument
                {
                    ItemType = document.ItemType,
                    ItemId = document.ItemId,
                    Title = document.Title,
                    Content = document.Content,
                    TagsText = document.TagsText,
                    UpdatedAt = document.UpdatedAt
                }).ToListAsync(cancellationToken);

            var rankCandidates = candidateDocuments
                .Select(document => new
                {
                    Document = document,
                    Rank = CalculateRank(document, terms)
                })
                .Where(candidate => candidate.Rank > 0)
                .OrderByDescending(candidate => candidate.Rank)
                .ThenByDescending(candidate => candidate.Document.UpdatedAt)
                .ThenBy(candidate => candidate.Document.ItemType)
                .ThenBy(candidate => candidate.Document.ItemId).ToList();

            var totalCount = rankCandidates.Count;

            // 預防性 => 避免極大 page 與 pageSize 相乘時 int overflow。
            var skip = (int)Math.Min((long)(normalizedPage - 1) * normalizedPageSize, int.MaxValue);

            var items = rankCandidates.Skip(skip).Take(normalizedPageSize)
                .Select(candidate =>
                    new SearchHit(candidate.Document.ItemType, candidate.Document.ItemId, candidate.Rank))
                .ToList();

            return new PagedResponse<SearchHit>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalPages = PagingHelper.CalculateTotalPages(totalCount, normalizedPageSize)
            };
        }

        // check ignore case
        private static bool ContainsIgnoreCase(string? source, string term)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                !string.IsNullOrWhiteSpace(term) &&
                source.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // validate term
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

            if(terms.ExpandedKeywords is null || terms.ExpandedKeywords.Count > MaxExpandedTerms)
            {
                throw new ArgumentException(
                    $"ExpandedKeywords 不可為 null，且最多 {MaxExpandedTerms} 個。", nameof(terms));
            }

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                terms.OriginalKeyword
            };

            foreach(var expandedKeyword in terms.ExpandedKeywords)
            {
                if(string.IsNullOrWhiteSpace(expandedKeyword) || expandedKeyword.Length > MaxKeywordLength ||
                    !string.Equals(expandedKeyword, expandedKeyword.Trim(), StringComparison.Ordinal) ||
                    !seenTerms.Add(expandedKeyword))
                {
                    throw new ArgumentException(
                        "ExpandedKeywords 不可包含空白、未 Trim、OriginalKeyword 或忽略大小寫後重複的字詞。",
                        nameof(terms));
                }
            }
        }

        private static int CalculateRank(SearchDocument document, SearchQueryTerms terms)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(terms);

            return
                ScoreField(document.Title, terms.OriginalKeyword, terms.ExpandedKeywords, 100, 40) +
                ScoreField(document.TagsText, terms.OriginalKeyword, terms.ExpandedKeywords, 60, 25) +
                ScoreField(document.Content, terms.OriginalKeyword, terms.ExpandedKeywords, 20, 10);
        }

        private static int ScoreField(string? fieldValue, string originalKeyword, IReadOnlyList<string> expandedKeywords,
            int originalWeight, int expandedWeight)
        {
            if(string.IsNullOrWhiteSpace(fieldValue))
            {
                return 0;
            }

            var score = ContainsIgnoreCase(fieldValue, originalKeyword) ? originalWeight : 0;

            foreach(var expandedKeyword in expandedKeywords)
            {
                if(ContainsIgnoreCase(fieldValue, expandedKeyword))
                {
                    score += expandedWeight;
                }
            }

            return score;
        }
    }
}
