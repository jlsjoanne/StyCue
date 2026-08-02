namespace Stycue.Api.Services.Models
{
    public class CommissionNotificationContext
    {
        public int RecipientUserId { get; init; }
        public int CommissionId { get; init; }
        public string CommissionTitle { get; init; } = string.Empty;
    }
}
