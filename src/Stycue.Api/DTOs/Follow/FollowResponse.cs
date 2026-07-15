namespace Stycue.Api.DTOs.Follow
{
    /// <summary>
    /// 追蹤或取消追蹤後的回應資料。
    /// </summary>
    public class FollowResponse
    {
        /// <summary>
        /// 被追蹤或取消追蹤的目標使用者 Id。
        /// </summary>
        public int TargetUserId { get; set; }

        /// <summary>
        /// 目前登入使用者是否已追蹤目標使用者。
        /// 追蹤成功時為 true，取消追蹤成功時為 false。
        /// </summary>
        public bool IsFollowing { get; set; }

        /// <summary>
        /// 目標使用者目前的粉絲總數。
        /// </summary>
        public int FollowerCount { get; set; }
    }
}
