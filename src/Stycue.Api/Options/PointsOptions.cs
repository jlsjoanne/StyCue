namespace Stycue.Api.Options
{
    public class PointsOptions
    {
        // 每日登入領取點數
        public int DailyRewardPoints { get; set; } = 10;

        // 註冊時贈送點數

        public int RegistrationRewardPoints { get; set; } = 50;

        // 建立委託時最低投入點數
        public int MinCommissionPoints { get; set; } = 50;

        // 加碼委託最低點數
        public int MinCommissionBoostPoints { get; set; } = 10;

        // 委託積分退還比例
        public int RefundPercent { get; set; } = 95;

        // 委託積分平台收取比例
        public int FeePercent { get; set; } = 5;

        // 預設到期天數
        public int DefaultCommissionExpireDays { get; set; } = 7;

        // repost / boost 預設延長天數
        public int DefaultCommissionExtendDays { get; set; } = 7;

        // 可以提前結束委託時間限定
        public int EarlyCloseLimitHours { get; set; } = 12;
    }
}
