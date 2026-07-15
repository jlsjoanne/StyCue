namespace Stycue.Api.Entities
{
    public class UserPointWallet
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int CurrentPoints { get; set; }
        public int LifetimeEarnedPoints { get; set; }
        public int LifetimeSpentPoints { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
