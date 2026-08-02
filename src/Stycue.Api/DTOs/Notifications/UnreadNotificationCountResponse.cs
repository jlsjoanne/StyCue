namespace Stycue.Api.DTOs.Notifications
{
    /// <summary>
    /// 目前登入使用者的未讀通知數量。
    /// </summary>
    public class UnreadNotificationCountResponse
    {
        /// <summary>
        /// 未讀通知筆數
        /// </summary>
        public int UnreadCount { get; set; }
    }
}
