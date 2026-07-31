namespace Stycue.Api.DTOs.Notifications
{
    /// <summary>
    /// 將目前登入使用者的所有未讀通知設為已讀後的結果。
    /// </summary>
    public class MarkAllNotificationsReadResponse
    {
        /// <summary>
        /// 本次實際由未讀更新為已讀的通知筆數。
        /// </summary>
        public int UpdatedCount { get; set; }
    }
}
