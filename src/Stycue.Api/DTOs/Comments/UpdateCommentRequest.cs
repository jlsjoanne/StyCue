using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Comments
{
    /// <summary>
    /// 更新留言請求
    /// </summary>
    public class UpdateCommentRequest
    {
        /// <summary>
        /// 更新留言內容
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 更新後保留或綁定的留言圖片 ID 清單
        /// </summary>
        public List<int> ImageIds { get; set; } = [];
    }
}
