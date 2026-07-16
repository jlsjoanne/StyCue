namespace Stycue.Api.Entities
{
    public class UserFollow
    {
        // FollowerUser = 主動追蹤別人的使用者
        public int FollowerUserId { get; set; }
        public User FollowerUser { get; set; } = null!;

        // FollowingUser = 被追蹤的使用者
        public int FollowingUserId { get; set; }
        public User FollowingUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
