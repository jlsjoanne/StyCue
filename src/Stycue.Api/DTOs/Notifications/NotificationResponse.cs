using System.Text.Json.Serialization;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Notifications
{
    /// <summary>
    /// 站內通知回應資料。
    /// </summary>
    public class NotificationResponse
    {
        /// <summary>
        /// 通知Id
        /// </summary>
        public int NotificationId { get; set; }

        /// <summary>
        /// 通知所代表的商業事件類型
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// 通知標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 通知訊息內容
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 觸發通知的使用者摘要；系統事件時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UserSummaryResponse? Actor { get; set; }

        /// <summary>
        /// 通知主要關聯資源的類型，供前端決定跳轉位置
        /// </summary>
        public NotificationReferenceType ReferenceType { get; set; }

        /// <summary>
        /// 通知主要關聯資源的Id；沒有特定關聯資源時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ReferenceId { get; set; }

        /// <summary>
        /// 通知是否已讀
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// 通知被設為已讀的 UTC 時間；未讀時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// 通知建立的 UTC 時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
