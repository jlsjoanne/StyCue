using System.Text.Json.Serialization;

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

        /// <summary>
        /// 目前登入使用者是否已追蹤此使用者。
        /// 未登入、此使用者為目前登入使用者本人，或此 API 未提供追蹤狀態時不回傳。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsFollowing { get; set; }
    }
}
