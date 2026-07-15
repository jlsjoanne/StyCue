namespace Stycue.Api.DTOs.Likes
{
    /// <summary>
    /// 按讚或取消按讚後的回應資料
    /// </summary>
    public class LikeResponse
    {
        /// <summary>
        /// 按讚目標類型
        /// comment、commission、post
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// 按讚目標 Id
        /// 依 TargetType 對應 CommentId、CommissionId 或 PostId
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// 目前登入使用者是否已對此目標按讚
        /// 按讚成功時為 true，取消按讚成功時為 false
        /// </summary>
        public bool IsLiked { get; set; }

        /// <summary>
        /// 此目標目前的按讚總數
        /// </summary>
        public int LikeCount { get; set; }
    }
}
