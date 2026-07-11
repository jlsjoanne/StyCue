namespace Stycue.Api.DTOs.Points
{
    /// <summary>
    /// 使用者積分錢包回應
    /// </summary>
    /// <remarks>
    /// 回傳目前可用積分，以及累計取得與花費積分。
    /// </remarks>
    public class PointWalletResponse
    {
        /// <summary>
        /// 目前可用積分。
        /// </summary>
        /// <example>50</example>
        public int CurrentPoints { get; set; }

        /// <summary>
        /// 累計取得積分。
        /// </summary>
        /// <example>300</example>
        public int LifetimeEarnedPoints { get; set; }

        /// <summary>
        /// 累計花費積分。
        /// </summary>
        /// <example>150</example>
        public int LifetimeSpentPoints { get; set; }

        /// <summary>
        /// 積分錢包最後更新時間。
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
