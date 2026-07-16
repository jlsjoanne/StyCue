using Stycue.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.Entities
{
    public class UserProfile
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public DateTime? BirthDate { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        public GenderIdentity? Gender { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
