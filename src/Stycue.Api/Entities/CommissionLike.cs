namespace Stycue.Api.Entities
{
    public class CommissionLike
    {
        public int CommissionId { get; set; }
        public Commission Commission { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
