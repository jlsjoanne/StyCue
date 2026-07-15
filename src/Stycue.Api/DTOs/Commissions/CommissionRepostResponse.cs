using Stycue.Api.DTOs.Images;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文補充內容回應
    /// </summary>
    public class CommissionRepostResponse
    {
        /// <summary>
        /// 委託文補充內容Id
        /// </summary>
        public int RepostId { get; set; }
        
        /// <summary>
        /// 原委託文Id
        /// </summary>
        public int CommissionId { get; set; }
        
        /// <summary>
        /// 補充內容
        /// </summary>
        public string SupplementContent { get; set; } = string.Empty;

        /// <summary>
        /// 補充追加的積分
        /// </summary>
        public int AdditionalPoints { get; set; }

        /// <summary>
        /// 補充內容建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 補充新增的圖片
        /// </summary>
        public IReadOnlyList<ImageResponse> Images { get; set; } = [];
    }
}
