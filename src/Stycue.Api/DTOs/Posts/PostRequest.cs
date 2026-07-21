using Stycue.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Posts
{
    /// <summary>
    /// 建立或更新貼文請求
    /// </summary>
    /// <remarks>
    /// 用於 POST /api/posts 與 PUT /api/posts/{postId}。
    /// PUT 採完整更新語意，前端需傳入更新後完整的 title、content、postType、imageIds 與 tagIds。
    /// </remarks>
    public class PostRequest
    {
        /// <summary>
        /// 貼文標題
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 貼文內容
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 貼文類型
        /// share = 分享文；question = 提問文
        /// </summary>
        [Required]
        public PostType PostType { get; set; }

        /// <summary>
        /// 穿搭風格
        /// </summary>
        [MaxLength(50)]
        public string? OutfitStyle { get; set; }

        /// <summary>
        /// 穿搭場合
        /// </summary>
        [MaxLength(50)]
        public string? OutfitOccasion { get; set; }

        /// <summary>
        ///  穿搭日期
        /// </summary>
        public DateOnly? OutfitDate { get; set; }

        /// <summary>
        /// 穿搭地點
        /// </summary>
        [MaxLength(100)]
        public string? OutfitLocation { get; set; }

        /// <summary>
        /// 已上傳的貼文圖片 ID 清單。
        /// 圖片需先透過 POST /api/images/posts 上傳取得 imageId。
        /// </summary>
        public List<int> ImageIds { get; set; } = [];

        /// <summary>
        /// 標籤 ID 清單。
        /// 標籤需先透過 Tags API 建立或取得 tagId。
        /// </summary>
        public List<int> TagIds { get; set; } = [];
    }
}
