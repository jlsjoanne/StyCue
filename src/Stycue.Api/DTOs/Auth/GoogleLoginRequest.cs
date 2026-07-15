using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Auth
{
    /// <summary>
    /// Google登入 - 請求
    /// </summary>
    public class GoogleLoginRequest
    {
        /// <summary>
        /// 從Google OAuth取得的ID Token
        /// </summary>
        [Required]
        public string IdToken { get; set; } = String.Empty;
    }
}
