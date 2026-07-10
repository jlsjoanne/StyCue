namespace Stycue.Api.Options
{
    public class PointsOptions
    {
        // 每日登入領取點數
        public int DailyRewardPoints { get; set; } = 50;

        // 建立委託時最低投入點數
        public int MinCommissionPoints { get; set; } = 50;

        // 加碼委託最低點數
        public int MinCommissionBoostPoints { get; set; } = 10;

        // 委託到期但沒留言時退還比例
        public int NoCommentRefundPercent { get; set; } = 90;

        // 委託到期但沒留言時平台收取比例
        public int NoCommentFeePercent { get; set; } = 10;

        // 預設到期天數
        public int DefaultCommissionExpireDays { get; set; } = 7;

        // repost / boost 最少延長天數
        public int MinCommissionExtendDays { get; set; } = 1;

        // repost / boost 最多延長天數
        public int MaxCommissionExtendDays { get; set; } = 7;
    }
}
