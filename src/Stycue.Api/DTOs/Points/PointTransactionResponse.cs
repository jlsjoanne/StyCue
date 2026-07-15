using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Points
{
    /// <summary>
    /// 積分交易紀錄回應
    /// </summary>
    public class PointTransactionResponse
    {
        /// <summary>
        /// 積分交易紀錄 Id
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// 更動多少積分。正數代表增加，負數代表扣除
        /// </summary>
        /// <example>50</example>
        public int Amount { get; set; }

        /// <summary>
        /// 積分交易類型
        /// 註冊贈送=1;
        /// 每日登入=2;
        /// 建立委託=3;
        /// 積分加碼=4;
        /// 最佳留言積分=5;
        /// 讚數最高留言積分=6;
        /// 退還積分=7;
        /// 積分手續費=8
        /// </summary>
        public PointTransactionType TransactionType { get; set; }

        /// <summary>
        /// 積分交易關聯來源類型
        /// 委託=1;
        /// 留言=2;
        /// 每日登入=3;
        /// 註冊=4;
        /// 其他 = 0
        /// </summary>
        /// 
        public PointReferenceType ReferenceType { get; set; }

        /// <summary>
        /// 回傳這筆交易實際關聯的資料 ID；沒有特定關聯時為 null (如每日登入或註冊送積分的狀況)
        /// </summary>
        public int? ReferenceId { get; set; }
        
        /// <summary>
        /// 積分交易描述
        /// </summary>
        /// <example>每日領取積分</example>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 積分交易建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
