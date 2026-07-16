namespace Stycue.Api.DTOs.Users
{
    /// <summary>
    /// 目前登入使用者的隱私個人資訊。
    /// </summary>
    public class PrivateUserInfoResponse
    {
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
