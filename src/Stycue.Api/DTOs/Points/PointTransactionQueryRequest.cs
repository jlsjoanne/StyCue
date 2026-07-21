using Stycue.Api.DTOs.Comm;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Points
{
    /// <summary>
    /// 積分交易紀錄查詢
    /// </summary>
    /// <remarks>
    /// 繼承分頁查詢參數，可依交易類型、來源類型、來源 ID 與時間區間篩選
    /// </remarks>
    public class PointTransactionQueryRequest : PagedQueryRequest
    {
        /// <summary>
        /// 積分交易類型篩選
        /// 註冊贈送 RegistrationReward=1;
        /// 每日登入 DailyReward=2;
        /// 建立委託 CommissionCreate=3;
        /// 積分加碼 CommissionBoost=4;
        /// 最佳留言積分 CommissionBestCommentReward=5;
        /// 讚數最高留言積分 CommissionAutoReward=6;
        /// 退還積分 CommissionRefund=7;
        /// 積分手續費 CommissionFee=8;
        /// 使用者購買積分 PointPurchase=9
        /// </summary>
        public PointTransactionType? TransactionType { get; set; }

        /// <summary>
        ///  積分交易關聯來源類型
        /// 委託 Commission =1;
        /// 留言 Comment=2;
        /// 每日登入 DailyClaim=3;
        /// 註冊 Registration =4;
        /// 積分購買訂單 PointPurchaseOrder=5;
        /// 其他 None = 0
        /// </summary>
        public PointReferenceType? ReferenceType { get; set; }

        /// <summary>
        /// 關聯來源資料 ID 篩選
        /// </summary>
        /// <remarks>
        /// 查詢某個特定來源資料的積分紀錄，例如查詢「委託文 Id=10」相關的所有積分異動
        /// </remarks>
        public int? ReferenceId { get; set; }

        /// <summary>
        /// 查詢起始時間
        /// </summary>
        public DateTime? StartAt { get; set; }

        /// <summary>
        /// 查詢結束時間
        /// </summary>
        public DateTime? EndAt { get; set; }
    }
}
