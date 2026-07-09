using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class ImageAsset
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int OwnerUserId { get; set; }
        public User OwnerUser { get; set; } = null!;
        public ImagePurpose Purpose { get; set; }
        public int? PostId { get; set; }
        public Post? Post { get; set; }
        public int? CommissionId { get; set; }
        public Commission? Commission { get; set; }
        public int? CommissionRepostId { get; set; }
        public CommissionRepost? CommissionRepost { get; set; }
        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        public ImageFashionMetadata? FashionMetadata { get; set; }
    }
}
