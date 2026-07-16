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
        /// 大頭貼圖片 Id。
        /// 未傳入時不更新。
        /// </summary>
        public int? AvatarImageId { get; set; }

        /// <summary>
        /// 自我介紹。
        /// 最多 500 個字元；未傳入時不更新。
        /// </summary>
        [MaxLength(500)]
        public string? Bio { get; set; }

        /// <summary>
        /// 性別認同。
        /// woman = 女；man = 男；nonBinary = 非二元；preferNotToSay = 不透露。
        /// 未傳入時不更新。
        /// </summary>
        public GenderIdentity? Gender { get; set; }

        /// <summary>
        /// 身高，單位 cm。
        /// 允許範圍 0 到 300；未傳入時不更新。
        /// </summary>
        [Range(0, 300)]
        public decimal? Height { get; set; }

        /// <summary>
        /// 體重，單位 kg。
        /// 允許範圍 0 到 500；未傳入時不更新。
        /// </summary>
        [Range(0, 500)]
        public decimal? Weight { get; set; }

        /// <summary>
        /// 生日。
        /// 未傳入時不更新。
        /// </summary>
        public DateTime? BirthDate { get; set; }
    }
}
