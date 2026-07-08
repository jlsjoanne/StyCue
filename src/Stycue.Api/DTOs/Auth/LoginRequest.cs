using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Auth
{
    /// <summary>
    /// 登入資料 - 請求
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 使用者Email，作為登入帳號
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = String.Empty;

        /// <summary>
        /// 使用者登入密碼
        /// </summary>
        [Required]
        public string Password { get; set; } = String.Empty;
    }
}
