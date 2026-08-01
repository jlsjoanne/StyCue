using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Notifications;
using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface INotificationService
    {
        // Notification Center API
        Task<ApiResponse<PagedResponse<NotificationResponse>>> GetMyNotificationsAsync(
            int userId, NotificationQueryRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<UnreadNotificationCountResponse>> GetUnreadCountAsync(
            int userId, CancellationToken cancellationToken = default);

        Task<ApiResponse<NotificationReadResponse>> MarkAsReadAsync(
            int userId, int notificationId, CancellationToken cancellationToken = default);

        Task<ApiResponse<MarkAllNotificationsReadResponse>> MarkAllAsReadAsync(
            int userId, CancellationToken cancellationToken = default);

        // Business notifications: 不由 Controller 直接呼叫

        Task CreateCommissionManualRewardGrantedAsync(
            CommissionNotificationContext commission, int actorUserId, int rewardPoints, CancellationToken cancellationToken = default);

        Task CreateCommissionAutomaticRewardGrantedAsync(
            CommissionNotificationContext commission, int rewardPoints, CancellationToken cancellationToken = default);

        Task CreateCommissionEarlyCloseRefundedAsync(
            CommissionNotificationContext commission, int refundPoints, CancellationToken cancellationToken = default);

        Task CreateCommissionExpiredWithoutCommentsRefundedAsync(
            CommissionNotificationContext commission, int refundPoints, CancellationToken cancellationToken = default);

        Task CreateCommissionExpiredAsync(
            CommissionNotificationContext commission, int expirationCycle, CancellationToken cancellationToken = default);

        Task CreateBestCommentSelectionRequiredAsync(
            CommissionNotificationContext commission, int expirationCycle, CancellationToken cancellationToken = default);

        Task CreateCommissionFirstExpirationActionRequiredAsync(
            CommissionNotificationContext commission, CancellationToken cancellationToken = default);

        Task CreateCommissionCommentCreatedAsync(
            CommissionNotificationContext commission, int actorUserId, int commentId, CancellationToken cancellationToken = default);

        Task CreatePointPurchaseSucceededAsync(
            int recipientUserId, int pointPurchaseOrderId, string productName, int points, CancellationToken cancellationToken = default);
    }
}
