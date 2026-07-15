using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 重新開啟委託文請求
    /// </summary>
    public class RepostCommissionRequest
    {
        /// <summary>
        /// 補充內容，不覆蓋原始委託內容
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string SupplementContent { get; set; } = string.Empty;

        /// <summary>
        /// 本次補充追加的積分
        /// </summary>
        [Range(0,int.MaxValue)]
        public int AdditionalPoints { get; set; }

        /// <summary>
        /// 本次補充新增的圖片 ID 清單
        /// </summary>
        public List<int> ImageIds { get; set; } = [];

        /// <summary>
        /// 本次補充新增或合併到委託文的標籤 ID 清單
        /// </summary>
        public List<int> TagIds { get; set; } = [];
    }
}
