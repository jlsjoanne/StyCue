using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Users
{
    /// <summary>
    /// 目前登入使用者的帳號基本資料。
    /// </summary>
    public class CurrentUserResponse
    {
        /// <summary>
        /// 使用者 Id。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 使用者暱稱。
        /// </summary>
        public string NickName { get; set; } = string.Empty;

        /// <summary>
        /// 使用者 Email。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 使用者角色。
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Email 是否已驗證。
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// 帳號建立時間。
        /// </summary>
        public DateTime CreatedAt { get; set; }


        /// <summary>
        /// 帳號最後更新時間。
        /// 未更新過時不回傳。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? UpdatedAt { get; set; }
    }
}
