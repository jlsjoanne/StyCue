namespace Stycue.Api.Enums
{
    /// <summary>
    /// 結算成功後的結果摘要
    /// </summary>
    public enum SettleRewardResultCode
    {
        /// <summary>
        /// 有留言，已發放積分給最佳或最高讚留言
        /// </summary>
        Awarded = 1,
        /// <summary>
        /// 沒有可獎勵留言，已退回部分積分並收取手續費
        /// </summary>
        Refunded = 2,
        /// <summary>
        /// 有留言資料但沒有符合結算條件的留言
        /// </summary>
        NoEligibleComment = 3
    }
}
