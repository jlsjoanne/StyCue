namespace Stycue.Api.DTOs.Notifications
{
    /// <summary>
    /// 單筆通知設為已讀後的結果。
    /// </summary>
    public class NotificationReadResponse
    {
        /// <summary>
        /// 通知Id
        /// </summary>
        public int NotificationId { get; set; }

        /// <summary>
        /// 通知是否已讀。
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// 通知被設為已讀的 UTC 時間；未讀時為 <c>null</c>。
        /// </summary>
        public DateTime? ReadAt { get; set; }
    }
}
