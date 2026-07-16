using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = String.Empty;

        [Required]
        [MaxLength(50)]
        public string NickName { get; set; } = String.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "User";

        [MaxLength(500)]
        public string? PasswordHash { get; set; }
        [MaxLength(100)]
        public string? GoogleSub { get; set; }

        public int? AvatarImageId { get; set; }

        public ImageAsset? AvatarImage { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public UserProfile? Profile { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeactivatedAt { get; set; }
    }
}
