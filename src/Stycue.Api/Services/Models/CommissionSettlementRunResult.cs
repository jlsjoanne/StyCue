namespace Stycue.Api.Services.Models
{
    public sealed class CommissionSettlementRunResult
    {
        public int CandidateCount { get; init; }
        public int ProcessedCount { get; init; }
        public int ExpiredNotificationCount { get; init; }
        public int SelectionReminderCount { get; init; }
        public int AutoRewardedCount { get; init; }
        public int RefundedCount { get; init; }
        public int SkippedCount { get; init; }
        public int FailedCount { get; init; }
    }
}
