using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Auth
{
    /// <summary>
    /// 註冊資料 - 請求
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// 使用者Email，作為註冊帳號
        /// </summary>
        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = String.Empty;

        /// <summary>
        /// 使用者暱稱
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string NickName { get; set; } = String.Empty;

        /// <summary>
        /// 使用者密碼
        /// </summary>
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = String.Empty;

        /// <summary>
        /// 二次確認密碼是否一致
        /// </summary>
        [Required]
        [Compare(nameof(Password))]
        public string PasswordCheck { get; set; } = String.Empty;
    }
}
