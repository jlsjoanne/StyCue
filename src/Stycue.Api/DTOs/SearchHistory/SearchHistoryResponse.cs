namespace Stycue.Api.DTOs.SearchHistory
{
    public class SearchHistoryResponse
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public DateTime SearchedAt { get; set; }
    }
}
