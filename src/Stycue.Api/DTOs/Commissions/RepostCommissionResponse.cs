using Stycue.Api.DTOs.Points;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 重新開啟委託文回應
    /// </summary>
    public class RepostCommissionResponse
    {
        /// <summary>
        /// 委託文Id
        /// </summary>
        public int CommissionId { get; set; }
        
        /// <summary>
        /// 重新補充內容Id
        /// </summary>
        public int RepostId { get; set; }

        /// <summary>
        /// 委託文狀態，{委託進行中=1, 已過期=2, 提前關閉=3, 積分已發放=4, 流標=5}
        /// </summary>
        public CommissionStatus Status { get; set; }

        /// <summary>
        /// 補充追加的積分
        /// </summary>
        public int AddedPoints { get; set; }
       
        /// <summary>
        /// 總懸賞積分
        /// </summary>
        public int TotalPoints { get; set; }
        
        /// <summary>
        /// 委託文到期時間
        /// </summary>
        public DateTime ExpiredAt { get; set; }
        
        /// <summary>
        /// 重新發表次數
        /// </summary>
        public int RepostCount { get; set; }
        
        /// <summary>
        /// 使用者積分狀態
        /// </summary>
        public PointWalletResponse Wallet { get; set; } = new();
    }
}
