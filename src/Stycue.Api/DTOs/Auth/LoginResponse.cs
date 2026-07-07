namespace Stycue.Api.DTOs.Auth
{
    /// <summary>
    /// 登入後回傳驗證資訊
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// JWT Access Token
        /// </summary>
        public string AccessToken { get; set; } = String.Empty;

        /// <summary>
        /// 使用者Email
        /// </summary>
        public string Email { get; set; } = String.Empty;

        /// <summary>
        /// 使用者暱稱
        /// </summary>
        public string NickName { get; set; } = String.Empty;

        /// <summary>
        /// 使用者角色
        /// </summary>
        public string Role { get; set; } = String.Empty;
    }
}
