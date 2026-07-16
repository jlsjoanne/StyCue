using System.Text.Json.Serialization;
using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.DTOs.Users
{
    /// <summary>
    /// 公開使用者個人資料。
    /// </summary>
    public class PublicUserProfileResponse
    {
        /// <summary>
        /// 使用者摘要資訊，包含使用者 Id、顯示名稱、大頭貼與追蹤狀態。
        /// </summary>
        public UserSummaryResponse User { get; set; } = new();

        /// <summary>
        /// 使用者自我介紹。
        /// 未設定時為 null。
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// 使用者追蹤中的人數。
        /// </summary>
        public int FollowingCount { get; set; }

        /// <summary>
        /// 使用者粉絲人數。
        /// </summary>
        public int FollowerCount { get; set; }

        /// <summary>
        /// 目前登入使用者是否已追蹤此使用者。
        /// 未登入、查詢自己或不提供追蹤狀態時不回傳。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsFollowing { get; set; }
    }
}
