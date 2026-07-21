namespace Stycue.Api.Services.Models
{
    public sealed class FullTextSearchHitRow
    {
        public int ItemType { get; set; }
        public int ItemId { get; set; }
        public int Rank { get; set; }
    }
}
