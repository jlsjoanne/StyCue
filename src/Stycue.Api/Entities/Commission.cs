using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class Commission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Title { get; set; } = String.Empty;
        public string Content { get; set; } = String.Empty;
        public CommissionStatus Status { get; set; } = CommissionStatus.Open;
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public int Age { get; set; }
        public string Budget { get; set; } = String.Empty;
        public int Points { get; set; }
        public int? AwardedCommentId { get; set; }
        public Comment? AwardedComment { get; set; }
        public DateTime? AwardedAt { get; set; }
        public DateTime? RewardSettledAt { get; set; }
        public int RepostCount { get; set; } // 目前：一個 Commission 只能 Repost 一次，因此此值只會是 0 或 1。
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public ICollection<ImageAsset> Images { get; set; } = new List<ImageAsset>();
        public ICollection<CommissionTag> CommissionTags { get; set; } = new List<CommissionTag>();
        public ICollection<CommissionLike> CommissionLikes { get; set; } = new List<CommissionLike>();
        public ICollection<CommissionFavorite> CommissionFavorites { get; set; } = new List<CommissionFavorite>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<CommissionRepost> Reposts { get; set; } = new List<CommissionRepost>();
    }
}
