using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.DTOs.Notifications
{
    /// <summary>
    /// 通知列表查詢條件。
    /// </summary>
    public class NotificationQueryRequest :PagedQueryRequest
    {
        /// <summary>
        /// 是否只取得未讀通知；預設為 <c>false</c>，回傳全部通知。
        /// </summary>
        public bool UnreadOnly { get; set; } = false;
    }
}
