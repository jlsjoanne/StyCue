namespace Stycue.Api.DTOs.Points
{
    /// <summary>
    /// 每日積分領取回應
    /// </summary>
    public class DailyPointClaimResponse
    {
        /// <summary>
        /// 是否已完成今日領取
        /// </summary>
        public bool IsClaimed { get; set; }

        /// <summary>
        /// 領取日期，日期基準使用台灣時區
        /// </summary>
        public DateOnly ClaimDate { get; set; }

        /// <summary>
        /// 本次領取的積分數
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// 領取後目前可用積分
        /// </summary>
        public int CurrentPoints { get; set; }

        /// <summary>
        /// 每日領取紀錄建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
