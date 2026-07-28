using Stycue.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Users
{
    /// <summary>
    /// 更新目前登入使用者個人資料的請求資料。
    /// </summary>
    public class UpdateUserProfileRequest
    {
        /// <summary>
        /// 使用者暱稱。
        /// 最多 50 個字元；未傳入時不更新。
        /// </summary>
        [MaxLength(50)]
        public string? NickName { get; set; }

        /// <summary>
        /// 自我介紹。
        /// 最多 500 個字元；未傳入或傳 null 時不更新，空字串或純空白字串會清空欄位。
        /// </summary>
        [MaxLength(500)]
        public string? Bio { get; set; }

        /// <summary>
        /// 性別認同。
        /// woman = 女；man = 男；nonBinary = 非二元；preferNotToSay = 不透露。
        /// 未傳入或傳null時不更新。
        /// </summary>
        public GenderIdentity? Gender { get; set; }

        /// <summary>
        /// 身高，單位 cm。
        /// 允許範圍 0 到 300；未傳入或傳null時不更新，空字串或純空白字串會清空欄位。
        /// </summary>
        public string? Height { get; set; }

        /// <summary>
        /// 體重，單位 kg。
        /// 允許範圍 0 到 500；未傳入或傳null時不更新，空字串或純空白字串會清空欄位。
        /// </summary>
        public string? Weight { get; set; }

        /// <summary>
        /// 生日，格式必須為 yyyy-MM-dd，例如 2000-01-31，空字串或純空白字串會清空欄位。
        /// 未傳入或傳null時不更新。
        /// </summary>
        public string? BirthDate { get; set; }
    }
}
