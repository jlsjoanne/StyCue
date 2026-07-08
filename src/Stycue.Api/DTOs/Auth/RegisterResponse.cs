namespace Stycue.Api.DTOs.Auth
{
    /// <summary>
    /// 註冊後回傳資訊
    /// </summary>
    public class RegisterResponse
    {
        /// <summary>
        /// 使用者註冊Email
        /// </summary>
        public string Email { get; set; } = String.Empty;

        /// <summary>
        /// 使用者註冊暱稱
        /// </summary>
        public string NickName { get; set; } = String.Empty;

        /// <summary>
        /// 使用者角色
        /// </summary>
        public string Role { get; set; } = String.Empty;

        /// <summary>
        /// 帳號建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
