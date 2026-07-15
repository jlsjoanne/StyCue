using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 建立委託文請求
    /// </summary>
    public class CreateCommissionRequest
    {
        /// <summary>
        /// 標題
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 內文
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 身高，單位cm
        /// </summary>
        [Range(1,300)]
        public decimal Height { get; set; }

        /// <summary>
        /// 體重，單位kg
        /// </summary>
        [Range(1,500)]
        public decimal Weight { get; set; }
        
        /// <summary>
        /// 年齡
        /// </summary>
        [Range(1,120)]
        public int Age { get; set; }

        /// <summary>
        /// 預算描述
        /// </summary>
        [MaxLength(100)]
        public string Budget { get; set; } = string.Empty;

        /// <summary>
        /// 委託懸賞積分，最低50積分
        /// </summary>
        [Range(50, int.MaxValue)]
        public int Points { get; set; }

        /// <summary>
        /// 已上傳的委託圖片 ID 清單
        /// </summary>
        public List<int> ImageIds { get; set; } = [];

        /// <summary>
        /// 標籤 ID 清單
        /// </summary>
        public List<int> TagIds { get; set; } = [];
    }
}
