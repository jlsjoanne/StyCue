using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.Entities
{
    public class SearchHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(100)]
        public string Keyword { get; set; } = string.Empty;
        public DateTime SearchedAt { get; set; }
    }
}
