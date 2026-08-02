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
            ValidateRunArgument(batchSize);
            ValidateSettlementSettings();

            var nowUtc = DateTime.UtcNow;

            var candidateCommissionIds = await FindCandidateCommissionIdsAsync(nowUtc, batchSize, cancellationToken);
            var processResults = new List<CommissionSettlementProcessResult>(candidateCommissionIds.Count);
            var failedCount = 0;

            foreach(var commissionId in candidateCommissionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var processResult = await ProcessCommissionAsync(commissionId, nowUtc, cancellationToken);

                    processResults.Add(processResult);
                }
                catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    failedCount += 1;

                    _logger.LogError(ex,
                        "Commission settlement processing failed. CommissionId: {CommissionId}",
                        commissionId);
                }
            }

            var runResult = BuildRunResult(candidateCommissionIds.Count, processResults, failedCount);

            _logger.LogInformation(
                "Commission settlement run completed. CandidateCount: {CandidateCount}, ProcessedCount: {ProcessedCount}," +
                " SkippedCount: {SkippedCount}, AutoRewardedCount: {AutoRewardedCount}, RefundedCount: {RefundedCount}, FailedCount: {FailedCount}",
                runResult.CandidateCount, runResult.ProcessedCount, runResult.SkippedCount,
                runResult.AutoRewardedCount, runResult.RefundedCount, runResult.FailedCount);

            return runResult;
        }

        // private

        // nested private records
        private sealed record CommissionSettlementProcessResult(
            int CommissionId,
             bool WasProcessed,
             bool ExpiredNotificationCreated,
             bool FirstExpirationActionReminderCreated,
             bool BestCommentSelectionReminderCreated,
             bool WasAutoRewarded,
             bool WasRefunded,
             bool WasSkipped);

        private sealed record ExpiredNotificationResult(
            bool ExpiredNotificationCreated, 
            bool FirstExpirationActionReminderCreated, bool BestCommentSelectionReminderCreated);

        private sealed record AutomaticRewardSettlementResult(
            int AwardedCommentId, int RecipientUserId, int RewardPoints, DateTime SettledAt);

        private sealed record NoCommentSettlementResult(
            int RecipientUserId, int RefundedPoints, DateTime SettledAt);

        private sealed class CommissionSettlementWriteException : Exception
        {
            public int CommissionId { get; }
            public string Operation { get; }
            public string? ErrorCode { get; }

            public CommissionSettlementWriteException(
                int commissionId, string operation, string? errorCode, string message) : base(message)
            {
                CommissionId = commissionId;
                Operation = operation;
                ErrorCode = errorCode;
            }
        }

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

            if( settings.FeePercent < 0 || settings.FeePercent >= 100)
            {
                throw new InvalidOperationException(
                    "Points:FeePercent 必須介於 0 與 99");
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
        
        // 積分計算helpers
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

        private int CalculateRewardPoints(int commissionPoints)
        {
            if(commissionPoints <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(commissionPoints),
                    "Commission points 必須大於 0");
            }

            var feePercent = _pointOptions.Value.FeePercent;

            // 預防性驗證，避免未來 helper 被單獨呼叫時產生不合法的退款交易
            if(feePercent < 0 || feePercent >= 100)
            {
                throw new InvalidOperationException("Points:FeePercent 必須介於 0 與 99");
            }

            var feePoints = (int)Math.Ceiling(commissionPoints * feePercent / 100m);
            var rewardPoints = commissionPoints - feePoints;

            if( rewardPoints <= 0)
            {
                throw new InvalidOperationException("扣除手續費後的獎勵積分必須大於 0");
            }

            return rewardPoints;
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

        private static bool IsNotificationDeduplicationUniqueConstraintViolation(DbUpdateException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            for(Exception? current = exception; current != null; current = current.InnerException)
            {
                if(current is SqlException sqlException &&
                    (sqlException.Number == 2601 || sqlException.Number == 2627) &&
                    sqlException.Message.Contains(
                        "UX_Notifications_RecipientUserId_DeduplicationKey", StringComparison.Ordinal))
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
                var expiredCreated = await _notificationService.CreateCommissionExpiredAsync(context, commission.ExpirationCycle, cancellationToken);
                var actionReminderCreated = await _notificationService.CreateCommissionFirstExpirationActionRequiredAsync(context, cancellationToken);

                return new ExpiredNotificationResult(expiredCreated, actionReminderCreated, false);
            }

            if(commission.ExpirationCycle == 2)
            {
                var expiredCreated = await _notificationService.CreateCommissionExpiredAsync(context, commission.ExpirationCycle, cancellationToken);
                
                if(eligibleRootComments.Count == 0)
                {
                    return new ExpiredNotificationResult(expiredCreated, false, false);
                }

                var selectionReminderCreated =  await _notificationService.CreateBestCommentSelectionRequiredAsync(context, commission.ExpirationCycle, cancellationToken);

                return new ExpiredNotificationResult(expiredCreated, false, selectionReminderCreated);
            }

            return new ExpiredNotificationResult(false, false, false);
        }

        // 結算寫入 helpers

        private async Task<AutomaticRewardSettlementResult> SettleAutomaticRewardAsync(
            Commission commission, Comment awardedComment, DateTime nowUtc, CancellationToken cancellationToken)
        {
            var rewardPoints = CalculateRewardPoints(commission.Points);

            // Point transaction
            var rewardResult = await _pointService.AddPointsAsync(
                awardedComment.UserId, rewardPoints, PointTransactionType.CommissionAutoReward,
                PointReferenceType.Commission, commission.Id, $"系統自動選出最佳留言，獲得委託積分：{commission.Title}",
                cancellationToken);

            if(!rewardResult.Success)
            {
                throw new CommissionSettlementWriteException(commission.Id, "AddAutomaticRewardPoints",
                    rewardResult.ErrorCode, rewardResult.Message);
            }

            // update commission status
            commission.Status = CommissionStatus.Rewarded;
            commission.AwardedCommentId = awardedComment.Id;
            commission.RewardSettledAt = nowUtc;
            commission.AwardedAt = nowUtc;
            commission.UpdatedAt = nowUtc;

            // add notification
            var notificationContext = BuildNotificationContext(commission, awardedComment.UserId);
            await _notificationService.CreateCommissionAutomaticRewardGrantedAsync(
                notificationContext, rewardPoints, cancellationToken);

            return new AutomaticRewardSettlementResult(awardedComment.Id, awardedComment.UserId,
                rewardPoints, nowUtc);

        }

        private async Task<NoCommentSettlementResult> SettleNoCommentCommissionAsync(
            Commission commission, DateTime nowUtc, CancellationToken cancellationToken)
        {
            var refundPoints = CalculateRefundPoints(commission.Points);

            var refundResult = await _pointService.AddPointsAsync(commission.UserId, refundPoints,
                PointTransactionType.CommissionRefund, PointReferenceType.Commission, commission.Id,
                $"委託到期未收到合格留言，退還委託積分：{commission.Title}", cancellationToken);

            if(!refundResult.Success)
            {
                throw new CommissionSettlementWriteException(commission.Id, "AddNoCommentRefundPoints",
                    refundResult.ErrorCode, refundResult.Message);
            }

            commission.Status = CommissionStatus.NoAward;
            commission.RewardSettledAt = nowUtc;
            commission.UpdatedAt = nowUtc;

            var notificationContext = BuildNotificationContext(commission, commission.UserId);
            await _notificationService.CreateCommissionExpiredWithoutCommentsRefundedAsync(
                notificationContext, refundPoints, cancellationToken);

            return new NoCommentSettlementResult(commission.UserId, refundPoints, nowUtc);

        }

        // 單筆流程 helper
        private async Task<CommissionSettlementProcessResult> ProcessCommissionAsync(
            int commissionId, DateTime nowUtc, CancellationToken cancellationToken)
        {
            var skippedResult = new CommissionSettlementProcessResult(commissionId,
                false, false, false, false, false, false, true);

            try
            {
                await using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
                {
                    try
                    {
                        var commission = await FindCommissionForSettlementUpdateAsync(commissionId, cancellationToken);

                        if (commission == null || !IsSettlementEligible(commission, nowUtc))
                        {
                            return skippedResult;
                        }

                        var eligibleRootComments = GetEligibleRootComments(commission);

                        var notificationResult = await EnsureExpiredNotificationsAsync(
                            commission, eligibleRootComments, cancellationToken);

                        if (IsWithinBestCommentSelectionGracePeriod(commission, nowUtc))
                        {
                            await _dbContext.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);

                            return new CommissionSettlementProcessResult(commission.Id,
                                true, notificationResult.ExpiredNotificationCreated, notificationResult.FirstExpirationActionReminderCreated,
                                notificationResult.BestCommentSelectionReminderCreated, false, false, false);
                        }

                        var wasAutoRewarded = false;
                        var wasRefunded = false;

                        if (eligibleRootComments.Count > 0)
                        {
                            var awardedComment = SelectAutomaticAwardComment(eligibleRootComments);

                            await SettleAutomaticRewardAsync(commission, awardedComment, nowUtc, cancellationToken);

                            wasAutoRewarded = true;
                        }
                        else
                        {
                            await SettleNoCommentCommissionAsync(commission, nowUtc, cancellationToken);

                            wasRefunded = true;
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        return new CommissionSettlementProcessResult(commission.Id,
                            true, notificationResult.ExpiredNotificationCreated, notificationResult.FirstExpirationActionReminderCreated,
                            notificationResult.BestCommentSelectionReminderCreated, wasAutoRewarded, wasRefunded,
                            false);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);

                        _logger.LogInformation(ex,
                            "Commission settlement lost concurrency race. CommissionId: {CommissionId}", commissionId);

                        return skippedResult;
                    }
                    catch(DbUpdateException ex) when (IsCommissionSettlementUniqueConstraintViolation(ex))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);

                        _logger.LogInformation(ex,
                            "Commission settlement duplicate transaction prevented. CommissionId: {CommissionId}", commissionId);

                        return skippedResult;
                    }
                    catch(DbUpdateException ex) when (IsNotificationDeduplicationUniqueConstraintViolation(ex))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);

                        _logger.LogInformation(ex,
                            "Commission settlement notification was already created by another instance. CommissionId: {CommissionId}",
                            commissionId);

                        return skippedResult;
                    }
                    catch(CommissionSettlementWriteException ex)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);

                        _logger.LogError(ex,
                            "Commission settlement write failed. CommissionId: {CommissionId}, Operation: {Operation}, ErrorCode: {ErrorCode}",
                            ex.CommissionId, ex.Operation, ex.ErrorCode);

                        throw;
                    }
                    catch
                    {
                        await transaction.RollbackAsync(CancellationToken.None);

                        throw;
                    }
                }
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }
        }

        private static CommissionSettlementRunResult BuildRunResult(
            int candidateCount, IReadOnlyCollection<CommissionSettlementProcessResult> processResults, int failedCount)
        {
            ArgumentNullException.ThrowIfNull(processResults);

            if( candidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(candidateCount),
                    "candidateCount 不可小於 0");
            }

            if( failedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedCount),
                    "failedCount 不可小於 0");
            }

            if( processResults.Count + failedCount != candidateCount)
            {
                throw new InvalidOperationException(
                    "本輪候選委託數與處理結果加失敗數不一致");
            }

            return new CommissionSettlementRunResult
            {
                CandidateCount = candidateCount,
                ProcessedCount = processResults.Count(x => x.WasProcessed),
                ExpiredNotificationCount = processResults.Count(x => x.ExpiredNotificationCreated),
                FirstExpirationActionReminderCount = processResults.Count(x => x.FirstExpirationActionReminderCreated),
                BestCommentSelectionReminderCount = processResults.Count(x => x.BestCommentSelectionReminderCreated),
                AutoRewardedCount = processResults.Count(x => x.WasAutoRewarded),
                RefundedCount = processResults.Count(x => x.WasRefunded),
                SkippedCount = processResults.Count(x => x.WasSkipped),
                FailedCount = failedCount
            };
        }
    }
}
