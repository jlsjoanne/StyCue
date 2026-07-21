using Stycue.Api.Services.Interfaces;
using Stycue.Api.Data;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Services.Models;

namespace Stycue.Api.Services
{
    public class FashionQueryExpander : IFashionQueryExpander
    {
        private readonly AppDbContext _dbContext;

        private const int MaxKeywordLength = 100;
        private const int MaxSearchTerms = 5;
        private const int MaxExpandedTerms = MaxSearchTerms - 1;

        public FashionQueryExpander(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SearchQueryTerms> ExpandAsync(
            string keyword, CancellationToken cancellationToken = default)
        {
            var normalizedKeyword = keyword?.Trim() ?? string.Empty;

            if(string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                throw new ArgumentException(
                    "搜尋關鍵字不可為空白", nameof(keyword));
            }

            if(normalizedKeyword.Length > MaxKeywordLength)
            {
                throw new ArgumentOutOfRangeException(nameof(keyword), "搜尋關鍵字不可超過 100 個字元");
            }

            var activeMappings = await _dbContext.FashionSearchDictionaries.AsNoTracking()
                .Where(item => item.IsActive)
                .ToListAsync(cancellationToken);

            var matchedCanonicalTerms = activeMappings
                .Where(item => ContainsIgnoreCase(normalizedKeyword, item.CanonicalTerm) || 
                    ContainsIgnoreCase(normalizedKeyword, item.Alias))
                .Select(item => item.CanonicalTerm)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var expansionMappings = activeMappings
                .Where(item => matchedCanonicalTerms.Contains(item.CanonicalTerm))
                .OrderByDescending(item => item.Weight)
                .ThenBy(item => item.CanonicalTerm).ThenBy(item => item.Alias);

            var expandedKeywords = new List<string>();
            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalizedKeyword };


            foreach(var item in expansionMappings)
            {
                AddExpansionTerm(expandedKeywords, seenTerms, item.CanonicalTerm);

                AddExpansionTerm(expandedKeywords, seenTerms, item.Alias);

                if(expandedKeywords.Count >= MaxExpandedTerms)
                {
                    break;
                }
            }

            return new SearchQueryTerms(
                normalizedKeyword, expandedKeywords);
        }

        private static void AddExpansionTerm(
            List<string> expandedKeywords, HashSet<string> seenTerms, string? value)
        {
            if( expandedKeywords.Count >= MaxExpandedTerms)
            {
                return;
            }

            var normalizedValue = value?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return;
            }

            if (seenTerms.Add(normalizedValue))
            {
                expandedKeywords.Add(normalizedValue);
            }
        }

        private static bool ContainsIgnoreCase(string source, string term)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                !string.IsNullOrWhiteSpace(term) &&
                source.Contains(term, StringComparison.OrdinalIgnoreCase);
        }
    }
}
