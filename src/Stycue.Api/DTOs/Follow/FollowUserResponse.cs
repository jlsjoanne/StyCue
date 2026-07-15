using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.DTOs.Follow
{
    /// <summary>
    /// 追蹤列表中的使用者資料。
    /// 可用於追蹤中列表或粉絲列表。
    /// </summary>
    public class FollowUserResponse
    {
        /// <summary>
        /// 使用者摘要資訊。
        /// 在追蹤中列表代表被目前使用者追蹤的使用者；
        /// 在粉絲列表代表追蹤目標使用者的使用者。
        /// </summary>
        public UserSummaryResponse User { get; set; } = new();

        /// <summary>
        /// 追蹤關係建立時間。
        /// </summary>
        public DateTime FollowedAt { get; set; }
    }
}
