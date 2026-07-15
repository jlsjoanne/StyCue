namespace Stycue.Api.DTOs.Comm
{
    /// <summary>
    /// 使用者摘要資訊
    /// </summary>
    public class UserSummaryResponse
    {
        /// <summary>
        /// 使用者Id (網頁畫面不顯示)
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 使用者顯示名稱
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// 使用者大頭貼Url，未設定時不回傳
        /// </summary>
        public string? AvatarUrl { get; set; }
    }
}
