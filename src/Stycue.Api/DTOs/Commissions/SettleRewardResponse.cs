using Stycue.Api.Enums;
using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文結算獎勵回應
    /// </summary>
    public class SettleRewardResponse
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
        /// 被獎勵留言Id
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AwardedCommentId { get; set; }

        /// <summary>
        /// 得到最高讚留言使用者Id
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RewardReceiverUserId { get; set; }

        /// <summary>
        /// 獎勵積分
        /// </summary>
        public int RewardPoints { get; set; }

        /// <summary>
        /// 退回積分
        /// </summary>
        public int RefundPoints { get; set; }

        /// <summary>
        /// 手續積分
        /// </summary>
        public int FeePoints { get; set; }

        /// <summary>
        /// 結算時間
        /// </summary>
        public DateTime SettledAt { get; set; }

        /// <summary>
        /// 委託文結果代碼
        /// </summary>
        public SettleRewardResultCode ResultCode { get; set; }
    }
}
