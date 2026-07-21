using System.ComponentModel.DataAnnotations;

using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? OutfitStyle { get; set; }

        [MaxLength(50)]
        public string? OutfitOccasion { get; set; }

        public DateOnly? OutfitDate { get; set; }

        [MaxLength(100)]
        public string? OutfitLocation { get; set; }
        public PostType PostType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<ImageAsset> Images { get; set; } = new List<ImageAsset>();
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public ICollection<PostFavorite> PostFavorites { get; set; } = new List<PostFavorite>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
