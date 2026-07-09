namespace Stycue.Api.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int? PostId { get; set; }
        public Post? Post { get; set; }
        public int? CommissionId { get; set; }
        public Commission? Commission { get; set; }
        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<ImageAsset> Images { get; set; } = new List<ImageAsset>();
        public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
