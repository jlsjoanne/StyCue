using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Comments
{
    /// <summary>
    /// 建立留言請求
    /// </summary>
    public class UpsertCommentRequest
    {
        /// <summary>
        /// 留言內容
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 已上傳的留言圖片 ID 清單
        /// </summary>
        public List<int> ImageIds { get; set; } = [];
    }
}
