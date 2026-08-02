namespace Stycue.Api.Options
{
    public sealed class CommissionSettlementOptions
    {
        public const string SectionName = "CommissionSettlement";

        // 是否註冊並啟動 Background Service
        public bool Enabled { get; init; } = false;

        // 正常排程執行間隔
        public int IntervalMinutes { get; init; } = 60;

        // API 啟動後等待多久才執行第一輪，避免部署瞬間增加 DB 負擔
        public int StartupDelaySeconds { get; init; } = 60;

        // 每輪最多處理的 Commission 筆數，避免資料量增加後單輪執行過久
        public int BatchSize { get; init; } = 50;
    }
}
