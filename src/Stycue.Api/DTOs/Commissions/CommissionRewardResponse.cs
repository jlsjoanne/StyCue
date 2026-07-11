using Stycue.Api.DTOs.Points;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文發放獎勵回應
    /// </summary>
    public class CommissionRewardResponse
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
        /// 被發放留言Id
        /// </summary>
        public int AwardedCommentId { get; set; }

        /// <summary>
        /// 被獎勵使用者Id
        /// </summary>
        public int RewardReceiverUserId { get; set; }

        /// <summary>
        /// 獎勵積分
        /// </summary>
        public int RewardPoints { get; set; }

        /// <summary>
        /// 獎勵發放時間
        /// </summary>
        public DateTime AwardedAt { get; set; }

        /// <summary>
        /// 被獎勵使用者積分狀態
        /// </summary>
        public PointWalletResponse ReceiverWallet { get; set; } = new();
    }
}
