using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Notifications;
using Stycue.Api.Services.Models;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Data;
using AutoMapper;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Entities;
using Stycue.Api.Enums;

namespace Stycue.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly IMapper _mapper;
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;

        public NotificationService(
            AppDbContext dbContext, ILogger<NotificationService> logger, 
            IMapper mapper, IUserSummaryResponseBuilder userSummaryResponseBuilder)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
        }

        // Notification Center API

        public async Task<ApiResponse<PagedResponse<NotificationResponse>>> GetMyNotificationsAsync(
            int userId, NotificationQueryRequest request, CancellationToken cancellationToken = default)
        {
            if( ValidateUserId<PagedResponse<NotificationResponse>>(userId) is { } userError)
            {
                return userError;
            }


            var (page, pageSize) = PagingHelper.Normalize(request.Page, request.PageSize);

            try
            {
                var query = _dbContext.Notifications.AsNoTracking()
                .Where(n => n.RecipientUserId == userId)
                .Include(n => n.ActorUser).ThenInclude(u => u!.AvatarImage)
                .AsQueryable();

                if (request.UnreadOnly)
                {
                    query = query.Where(n => n.IsRead == false);
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .ThenByDescending(n => n.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

                var response = new PagedResponse<NotificationResponse>
                {
                    Items = notifications.Select(MapResponse).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = PagingHelper.CalculateTotalPages(totalCount, pageSize)
                };

                return ApiResponse<PagedResponse<NotificationResponse>>.SuccessResult(response, "取得通知成功");
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Get notifications failed. UserId: {UserId}",
                    userId);

                return ApiResponse<PagedResponse<NotificationResponse>>.FailResult(
                    "取得通知失敗，請稍後再試", "NOTIFICATION_QUERY_FAILED");
            }
        }

        public async Task<ApiResponse<UnreadNotificationCountResponse>> GetUnreadCountAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            if( ValidateUserId<UnreadNotificationCountResponse>(userId) is { } userError)
            {
                return userError;
            }

            try
            {
                var unreadCount = await _dbContext.Notifications.AsNoTracking()
                .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

                var response = new UnreadNotificationCountResponse
                {
                    UnreadCount = unreadCount
                };

                return ApiResponse<UnreadNotificationCountResponse>.SuccessResult(response, "取得未讀通知數成功");
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Get unread notification count failed. UserId: {UserId}", userId);

                return ApiResponse<UnreadNotificationCountResponse>.FailResult(
                    "取得未讀通知數失敗，請稍後再試", "NOTIFICATION_UNREAD_COUNT_FAILED");
            }
            
        }

        public async Task<ApiResponse<NotificationReadResponse>> MarkAsReadAsync(
            int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<NotificationReadResponse>(userId) is { } userError)
            {
                return userError;
            }

            if(notificationId <= 0)
            {
                return ApiResponse<NotificationReadResponse>.FailResult("不合法的通知 ID", "INVALID_NOTIFICATION_ID");
            }

            try
            {
                var notification = await FindOwnedNotificationAsync(userId, notificationId, cancellationToken);

                if (notification == null)
                {
                    return ApiResponse<NotificationReadResponse>.FailResult("找不到指定通知", "NOTIFICATION_NOT_FOUND");
                }

                if(!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                var response = new NotificationReadResponse
                {
                    NotificationId = notification.Id,
                    IsRead = notification.IsRead,
                    ReadAt = notification.ReadAt
                };

                return ApiResponse<NotificationReadResponse>.SuccessResult(response, "通知已設為已讀");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Mark notification as read failed. UserId: {UserId}, NotificationId: {NotificationId}",
                    userId, notificationId);

                return ApiResponse<NotificationReadResponse>.FailResult(
                    "更新通知已讀狀態失敗，請稍後再試", "NOTIFICATION_MARK_READ_FAILED");
            }
        }

        public async Task<ApiResponse<MarkAllNotificationsReadResponse>> MarkAllAsReadAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            if(ValidateUserId<MarkAllNotificationsReadResponse>(userId) is { } userError)
            {
                return userError;
            }

            try
            {
                var readAt = DateTime.UtcNow;

                var updatedCount = await _dbContext.Notifications
                    .Where(n => n.RecipientUserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, readAt), cancellationToken);

                var response = new MarkAllNotificationsReadResponse
                {
                    UpdatedCount = updatedCount
                };

                return ApiResponse<MarkAllNotificationsReadResponse>.SuccessResult(response, "所有未讀通知已設為已讀");

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                    "Mark all notifications as read failed. UserId: {UserId}",
                    userId);

                return ApiResponse<MarkAllNotificationsReadResponse>.FailResult(
                    "更新全部通知已讀狀態失敗，請稍後再試", "NOTIFICATION_MARK_ALL_READ_FAILED");
            }
        }

        // Business notifications

        public async Task CreateCommissionManualRewardGrantedAsync(
            CommissionNotificationContext commission, int actorUserId, int rewardPoints, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);
            ValidatePositivePoints(rewardPoints, nameof(rewardPoints));

            if( actorUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actorUserId), "ActorUserId 必須大於 0");
            }

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: actorUserId,
                Type: NotificationType.CommissionRewardGrantedManual,
                Title: "你獲得最佳留言獎勵",
                Message: $"你在委託「{commission.CommissionTitle}」的留言被選為最佳留言，已獲得 {rewardPoints} 點。",
                ReferenceType: NotificationReferenceType.Commission,
                ReferenceId: commission.CommissionId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, "reward:manual"));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        public async Task CreateCommissionAutomaticRewardGrantedAsync(
            CommissionNotificationContext commission, int rewardPoints, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);
            ValidatePositivePoints(rewardPoints, nameof(rewardPoints));

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: null,
                Type: NotificationType.CommissionRewardGrantedAutomatic,
                Title: "你獲得最高讚留言獎勵",
                Message: $"委託「{commission.CommissionTitle}」已完成結算，你的留言獲得最高讚，已獲得 {rewardPoints} 點。",
                ReferenceType: NotificationReferenceType.Commission,
                ReferenceId: commission.CommissionId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, "reward:automatic"));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        public async Task CreateCommissionEarlyCloseRefundedAsync(
            CommissionNotificationContext commission, int refundPoints, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);
            ValidatePositivePoints(refundPoints, nameof(refundPoints));

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: null,
                Type: NotificationType.CommissionPointsRefunded,
                Title: "委託已關閉，點數已退還",
                Message: $"你的委託「{commission.CommissionTitle}」已提前關閉，已退還 {refundPoints} 點。",
                ReferenceType: NotificationReferenceType.Commission,
                ReferenceId: commission.CommissionId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, "refund:early-close"));

            await AddIfNotExistsAsync(command, cancellationToken);

        }

        public async Task CreateCommissionExpiredWithoutCommentsRefundedAsync(
            CommissionNotificationContext commission, int refundPoints, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);
            ValidatePositivePoints(refundPoints, nameof(refundPoints));

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: null,
                Type: NotificationType.CommissionPointsRefunded,
                Title: "委託到期，點數已退還",
                Message: $"你的委託「{commission.CommissionTitle}」已到期且沒有合格留言，已退還 {refundPoints} 點。",
                ReferenceType: NotificationReferenceType.Commission,
                ReferenceId: commission.CommissionId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, "refund:expired-no-comment"));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        public async Task CreateCommissionExpiredAsync(
            CommissionNotificationContext commission, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: null,
                Type: NotificationType.CommissionExpired,
                Title: "委託文已到期",
                Message: $"你的委託「{commission.CommissionTitle}」已到期",
                ReferenceType: NotificationReferenceType.Commission,
                ReferenceId: commission.CommissionId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, "expired"));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        public async Task CreateBestCommentSelectionRequiredAsync(
            CommissionNotificationContext commission, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: null,
                Type: NotificationType.CommissionBestCommentSelectionRequired,
                Title: "請在 24 小時內選擇最佳留言",
                Message: $"你的委託「{commission.CommissionTitle}」已到期，請在 24 小時內選擇最佳留言",
                ReferenceType: NotificationReferenceType.Commission,
                ReferenceId: commission.CommissionId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, "best-comment-selection-required"));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        public async Task CreateCommissionCommentCreatedAsync(
            CommissionNotificationContext commission, 
            int actorUserId, int commentId, CancellationToken cancellationToken = default)
        {
            ValidateCommissionNotificationContext(commission);

            if( actorUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actorUserId),
                    "ActorUserId 必須大於 0");
            }

            if( commentId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(commentId),
                    "CommentId 必須大於 0");
            }

            if( commission.RecipientUserId == actorUserId)
            {
                return;
            }

            var command = new NotificationCreateCommand(
                RecipientUserId: commission.RecipientUserId,
                ActorUserId: actorUserId,
                Type: NotificationType.CommissionCommentCreated,
                Title: "你的委託有新留言",
                Message: $"你的委託「{commission.CommissionTitle}」收到了一則新留言。",
                ReferenceType: NotificationReferenceType.Comment,
                ReferenceId: commentId,
                DeduplicationKey: BuildCommissionDeduplicationKey(commission.CommissionId, $"comment:{commentId}:created"));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        public async Task CreatePointPurchaseSucceededAsync(
            int recipientUserId, int pointPurchaseOrderId, string productName, int points, 
            CancellationToken cancellationToken = default)
        {
            if( recipientUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(recipientUserId), "RecipientUserId 必須大於 0");
            }

            if( pointPurchaseOrderId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pointPurchaseOrderId), "PointPurchaseOrderId 必須大於 0");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new ArgumentException("ProductName 不可為空", nameof(productName));
            }

            ValidatePositivePoints(points, nameof(points));

            var command = new NotificationCreateCommand(
                RecipientUserId: recipientUserId,
                ActorUserId: null,
                Type: NotificationType.PointPurchaseSucceeded,
                Title: "點數購買成功",
                Message: $"你已成功購買「{productName}」，{points} 點已存入帳戶。",
                ReferenceType: NotificationReferenceType.PointPurchaseOrder,
                ReferenceId: pointPurchaseOrderId,
                DeduplicationKey: BuildPointPurchaseDeduplicationKey(pointPurchaseOrderId));

            await AddIfNotExistsAsync(command, cancellationToken);
        }

        // private helpers

        // validation
        private static void ValidateCommissionNotificationContext(CommissionNotificationContext commission)
        {
            ArgumentNullException.ThrowIfNull(commission);

            if( commission.RecipientUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(commission.RecipientUserId), "RecipientUserId 必須大於 0");
            }

            if( commission.CommissionId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(commission.CommissionId), "CommissionId 必須大於 0");
            }

            if(string.IsNullOrWhiteSpace(commission.CommissionTitle))
            {
                throw new ArgumentException("Commission Title 不可為空", nameof(commission.CommissionTitle));
            }
        }

        private static void ValidatePositivePoints(int points, string parameterName)
        {
            if(points <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "點數必須大於 0");
            }
        }

        private static ApiResponse<T>? ValidateUserId<T>(int userId)
        {
            return userId > 0 ? null : ApiResponse<T>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
        } 

        private sealed record NotificationCreateCommand(
            int RecipientUserId,
            int? ActorUserId,
            NotificationType Type,
            string Title,
            string Message,
            NotificationReferenceType ReferenceType,
            int? ReferenceId,
            string DeduplicationKey);

        private async Task AddIfNotExistsAsync(NotificationCreateCommand command, CancellationToken cancellationToken)
        {
            if(command.RecipientUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(command.RecipientUserId), "RecipientUserId 必須大於 0");
            }

            if (string.IsNullOrWhiteSpace(command.DeduplicationKey) || command.DeduplicationKey.Length > 200)
            {
                throw new ArgumentException(
                    "DeduplicationKey 不可為空，且長度不得超過 200",
                    nameof(command.DeduplicationKey));
            }

            if( command.ReferenceType == NotificationReferenceType.None && command.ReferenceId.HasValue)
            {
                throw new ArgumentException(
                    "ReferenceType 為 None 時，ReferenceId 必須為 null", nameof(command.ReferenceId));
            }

            if(command.ReferenceType != NotificationReferenceType.None && !command.ReferenceId.HasValue)
            {
                throw new ArgumentException(
                    "ReferenceType 不為 None 時，ReferenceId 不可為 null", nameof(command.ReferenceId));
            }

            // EF Core 的 ChangeTracker 會保留目前這個 AppDbContext 已追蹤、但可能尚未寫入資料庫的 Entity
            // 若沒有這段，第二次的 AnyAsync 只查 SQL Server，仍找不到第一筆尚未 INSERT 的資料，
            // 會再加入一筆，最後在 SaveChangesAsync() 時被 unique index 擋下。
            // 這個檢查只保護同一個 AppDbContext
            var isAlreadyTracked = _dbContext.ChangeTracker
                .Entries<Notification>()
                .Any(entry => entry.State != EntityState.Deleted && entry.Entity.RecipientUserId == command.RecipientUserId &&
                    entry.Entity.DeduplicationKey == command.DeduplicationKey);

            if(isAlreadyTracked)
            {
                return;
            }

            var alreadyExists = await _dbContext.Notifications.AsNoTracking()
                .AnyAsync(n => n.RecipientUserId == command.RecipientUserId && n.DeduplicationKey == command.DeduplicationKey, cancellationToken);

            if (alreadyExists)
            {
                return;
            }

            _dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = command.RecipientUserId,
                ActorUserId = command.ActorUserId,
                Type = command.Type,
                Title = command.Title,
                Message = command.Message,
                ReferenceType = command.ReferenceType,
                ReferenceId = command.ReferenceId,
                IsRead = false,
                ReadAt = null,
                CreatedAt = DateTime.UtcNow,
                DeduplicationKey = command.DeduplicationKey
            });
        }

        // 產生固定 key

        private static string BuildCommissionDeduplicationKey(int commissionId, string eventName)
        {
            return $"commission:{commissionId}:{eventName}";
        }

        private static string BuildPointPurchaseDeduplicationKey(int pointPurchaseOrderId)
        {
            return $"point-purchase:{pointPurchaseOrderId}:succeeded";
        }

        // Map Notification Response
        private NotificationResponse MapResponse(Notification notification)
        {
            var response = _mapper.Map<NotificationResponse>(notification);

            if( notification.ActorUser != null)
            {
                response.Actor = _userSummaryResponseBuilder.Build(notification.ActorUser);
            }

            return response;
        }

        private async Task<Notification?> FindOwnedNotificationAsync(int userId, int notificationId, CancellationToken cancellationToken)
        {
            return await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, cancellationToken);
        }
    }
}
