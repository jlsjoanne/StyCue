namespace Stycue.Api.Services.Models
{
    public sealed record SearchQueryTerms(
        string OriginalKeyword, IReadOnlyList<string> ExpandedKeywords);
}
