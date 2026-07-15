using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 關閉委託文回應
    /// </summary>
    public class CloseCommissionResponse
    {
        /// <summary>
        /// 委託文Id
        /// </summary>
        public int CommissionId { get; set; }

        /// <summary>
        /// 委託文狀態，{委託進行中=1, 已過期=2, 提前關閉=3, 積分已發放=4, 流標=5}
        /// </summary>
        public CommissionStatus Status { get; set; }
        
        /// <summary>
        /// 委託文關閉時間
        /// </summary>
        public DateTime ClosedAt { get; set; }

        /// <summary>
        /// 提前關閉退回積分，無條件進位
        /// </summary>
        public int RefundedPoints { get; set; }
        
        /// <summary>
        /// 提前關閉手續積分
        /// </summary>
        public int FeePoints { get; set; }
    }
}
