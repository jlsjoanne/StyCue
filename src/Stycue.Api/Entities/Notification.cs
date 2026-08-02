using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int RecipientUserId { get; set; }

        public User RecipientUser { get; set; } = null!;
        public int? ActorUserId { get; set; }   

        public User? ActorUser { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationReferenceType ReferenceType { get; set;}
        public int? ReferenceId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // 同一個商業事件的固定識別碼，例如：commission:{commissionId}:reward:manual
        // 跟RecipientUserId是Unique => 同一收件人不能收到同一商業事件兩次
        public string DeduplicationKey { get; set; } = null!;
    }
}
