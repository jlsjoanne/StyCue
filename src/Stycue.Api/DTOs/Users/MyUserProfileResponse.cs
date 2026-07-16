using Stycue.Api.DTOs.Comm;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Users
{
    /// <summary>
    /// 目前登入使用者的個人資料。
    /// </summary>
    public class MyUserProfileResponse
    {
        /// <summary>
        /// 使用者摘要資訊，包含使用者 Id、顯示名稱與大頭貼。
        /// </summary>
        public UserSummaryResponse User { get; set; } = new();

        /// <summary>
        /// 使用者自我介紹。
        /// 未設定時為 null。
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// 性別認同。
        /// woman = 女；man = 男；nonBinary = 非二元；preferNotToSay = 不透露。
        /// 未設定時為 null。
        /// </summary>
        public GenderIdentity? Gender { get; set; }

        /// <summary>
        /// 身高，單位 cm。
        /// 未設定時為 null。
        /// </summary>
        public decimal? Height { get; set; }

        /// <summary>
        /// 體重，單位 kg。
        /// 未設定時為 null。
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// 生日。
        /// 未設定時為 null。
        /// </summary>
        public DateTime? BirthDate { get; set; }
    }
}
