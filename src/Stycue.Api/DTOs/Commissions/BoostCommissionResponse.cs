using Stycue.Api.DTOs.Points;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文加碼回應
    /// </summary>
    public class BoostCommissionResponse
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
        /// 加碼積分
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
        /// 使用者積分狀態
        /// </summary>
        public PointWalletResponse Wallet { get; set; } = new();
    }
}
