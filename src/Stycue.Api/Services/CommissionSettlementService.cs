using Stycue.Api.Services.Models;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Data;
using Stycue.Api.Options;
using Stycue.Api.Enums;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Stycue.Api.Services
{
    public class CommissionSettlementService : ICommissionSettlementService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPointService _pointService;
        private readonly INotificationService _notificationService;
        private readonly IOptions<PointsOptions> _pointOptions;
        private readonly ILogger<CommissionSettlementService> _logger;

        public CommissionSettlementService(
            AppDbContext dbContext, IPointService pointService, 
            INotificationService notificationService, IOptions<PointsOptions> pointOptions,
            ILogger<CommissionSettlementService> logger)
        {
            _dbContext = dbContext;
            _pointService = pointService;
            _notificationService = notificationService;
            _pointOptions = pointOptions;
            _logger = logger;
        }

        public async Task<CommissionSettlementRunResult> RunOnceAsync(
            // place holder
            int batchSize, CancellationToken cancellationToken)
        {
            return new CommissionSettlementRunResult();
        }

        // private

        // nested private records
        private sealed record CommissionSettlementProcessResult(
            int CommissionId,
             bool WasProcessed,
             bool ExpiredNotificationHandled,
             bool FirstExpirationActionReminderHandled,
             bool BestCommentSelectionReminderHandled,
             bool WasAutoRewarded,
             bool WasRefunded,
             bool WasSkipped);

        private sealed record ExpiredNotificationResult(
            bool ExpiredNotificationHandled, bool FirstExpirationActionReminderHandled, bool BestCommentSelectionReminderHandled);

        // private helpers
        
        // Validate
        private static void ValidateRunArgument(int batchSize)
        {
            if(batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize),
                    "batchSize 必須大於 0");
            }
        }

        private void ValidateSettlementSettings()
        {
            var settings = _pointOptions.Value;

            if(settings.BestCommentSelectionGraceHours <= 0)
            {
                throw new InvalidOperationException(
                    "Points:BestCommentSelectionGraceHours 必須大於 0");
            }

            if( settings.RefundPercent <= 0 || settings.RefundPercent > 100)
            {
                throw new InvalidOperationException(
                    "Points:RefundPercent 必須介於 1 與 100");
            }

            if( settings.FeePercent < 0 || settings.FeePercent > 100)
            {
                throw new InvalidOperationException(
                    "Points:FeePercent 必須介於 0 與 100");
            }
        }

        // 此筆委託是否仍可由 background service 處理
        // 處理已到期 + 尚未結算 + 未關閉 + ExpirationCycle 為 1 或 2
        private static bool IsSettlementEligible(Commission commission, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(commission);

            return (commission.ExpirationCycle == 1 || commission.ExpirationCycle == 2) &&
                commission.ExpiredAt <= now &&
                (commission.Status == CommissionStatus.Open || commission.Status == CommissionStatus.Expired) &&
                commission.ClosedAt == null &&
                commission.AwardedCommentId == null &&
                commission.AwardedAt == null &&
                commission.RewardSettledAt == null;
        }

        private bool IsWithinBestCommentSelectionGracePeriod(Commission commission, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(commission);

            var graceHours = _pointOptions.Value.BestCommentSelectionGraceHours;

            return commission.ExpiredAt <= now && now <= commission.ExpiredAt.AddHours(graceHours);
        }

        // get values

        private static IReadOnlyList<Comment> GetEligibleRootComments(Commission commission)
        {
            ArgumentNullException.ThrowIfNull(commission);

            return commission.Comments
                .Where(c => c.DeletedAt == null && c.ParentCommentId == null &&
                    c.CreatedAt < commission.ExpiredAt).ToList();
        }

        private static Comment SelectAutomaticAwardComment(
            IReadOnlyCollection<Comment> eligibleRootComments)
        {
            ArgumentNullException.ThrowIfNull(eligibleRootComments);

            // 預防性防呆驗證
            // 理論上在ProcessCommissionAsync時，
            // 若eligibleRootComments.Count == 0 => 執行SettleNoCommentCommissionAsync
            if (eligibleRootComments.Count == 0)
            {
                throw new InvalidOperationException("沒有可供自動發獎的候選留言");
            }

            return eligibleRootComments.OrderByDescending(c => c.CommentLikes.Count)
                .ThenBy(c => c.CreatedAt).ThenBy(c => c.Id).First();
        }

        private int CalculateRefundPoints(int commissionPoints)
        {
            if( commissionPoints <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(commissionPoints),
                    "Commission points 必須大於 0");
            }

            var refundPercent = _pointOptions.Value.RefundPercent;

            // 預防性驗證，避免未來 helper 被單獨呼叫時產生不合法的退款交易
            if (refundPercent <= 0 || refundPercent > 100)
            {
                throw new InvalidOperationException("Points:RefundPercent 必須介於 1 與 100");
            }

            var refundPoints = (int)Math.Ceiling(commissionPoints * refundPercent / 100m);

            if( refundPoints <= 0)
            {
                throw new InvalidOperationException("計算後的退款點數必須大於 0");
            }

            return refundPoints;
        }


        private static CommissionNotificationContext BuildNotificationContext(
            Commission commission, int recipientUserId)
        {
            ArgumentNullException.ThrowIfNull(commission);

            return new CommissionNotificationContext
            {
                RecipientUserId = recipientUserId,
                CommissionId = commission.Id,
                CommissionTitle = commission.Title
            };
        }

        // db exception check
        private static bool IsCommissionSettlementUniqueConstraintViolation(DbUpdateException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            for( Exception? current = exception; current != null; current = current.InnerException)
            {
                if(current is SqlException sqlException &&
                    (sqlException.Number == 2601 || sqlException.Number == 2627) &&
                    sqlException.Message.Contains("UX_PointTransactions_CommissionSettlement", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // 資料讀取 helper

        private async Task<IReadOnlyList<int>> FindCandidateCommissionIdsAsync(
            DateTime now, int batchSize, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions.AsNoTracking()
                .Where(c =>
                    (c.ExpirationCycle == 1 || c.ExpirationCycle == 2) &&
                    c.ExpiredAt <= now &&
                    (c.Status == CommissionStatus.Open || c.Status == CommissionStatus.Expired) &&
                    c.ClosedAt == null && c.AwardedCommentId == null && c.AwardedAt == null &&
                    c.RewardSettledAt == null)
                .OrderBy(c => c.ExpiredAt).ThenBy(c => c.Id)
                .Take(batchSize).Select(c => c.Id).ToListAsync(cancellationToken);
        }

        private async Task<Commission?> FindCommissionForSettlementUpdateAsync(
            int commissionId, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions.AsSplitQuery()
                .Include(c => c.Comments).ThenInclude(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);
        }

        // 通知 helper
        private async Task<ExpiredNotificationResult> EnsureExpiredNotificationsAsync(
            Commission commission, IReadOnlyCollection<Comment> eligibleRootComments, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(commission);
            ArgumentNullException.ThrowIfNull(eligibleRootComments);

            var context = BuildNotificationContext(commission, commission.UserId);

            if(commission.ExpirationCycle == 1)
            {
                await _notificationService.CreateCommissionExpiredAsync(context, commission.ExpirationCycle, cancellationToken);
                await _notificationService.CreateCommissionFirstExpirationActionRequiredAsync(context, cancellationToken);

                return new ExpiredNotificationResult(true, true, false);
            }

            if(commission.ExpirationCycle == 2)
            {
                await _notificationService.CreateCommissionExpiredAsync(context, commission.ExpirationCycle, cancellationToken);
                
                if(eligibleRootComments.Count == 0)
                {
                    return new ExpiredNotificationResult(true, false, false);
                }

                await _notificationService.CreateBestCommentSelectionRequiredAsync(context, commission.ExpirationCycle, cancellationToken);

                return new ExpiredNotificationResult(true, false, true);
            }

            return new ExpiredNotificationResult(false, false, false);
        }
    }
}
