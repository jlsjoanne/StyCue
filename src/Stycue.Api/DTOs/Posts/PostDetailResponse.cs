using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Posts
{
    /// <summary>
    /// 貼文詳情回應。
    /// </summary>
    public class PostDetailResponse
    {
        /// <summary>
        /// 貼文 ID。
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 發表者摘要。
        /// </summary>
        public UserSummaryResponse Author { get; set; } = new();

        /// <summary>
        /// 貼文標題。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 貼文完整內容。
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 貼文類型。
        /// share = 分享文；question = 提問文。
        /// </summary>
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
        /// 建立時間。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最後更新時間。
        /// 未更新過時不回傳此欄位。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? UpdatedAt { get; set; }


        /// <summary>
        /// 目前登入使用者是否為貼文作者。
        /// </summary>
        public bool IsOwner { get; set; }

        /// <summary>
        /// 目前登入使用者是否可編輯此貼文。
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// 目前登入使用者是否可刪除此貼文。
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// 按讚數。
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 留言數。
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 收藏數。
        /// </summary>
        public int FavoriteCount { get; set; }

        /// <summary>
        /// 目前登入使用者是否已按讚。
        /// 未登入時不回傳此欄位。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsLiked { get; set; }

        /// <summary>
        /// 目前登入使用者是否已收藏。
        /// 未登入時不回傳此欄位。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsFavorited { get; set; }

        /// <summary>
        /// 貼文圖片清單。
        /// </summary>
        public IReadOnlyList<ImageResponse> Images { get; set; } = [];

        /// <summary>
        /// 貼文標籤清單。
        /// </summary>
        public IReadOnlyList<TagResponse> Tags { get; set; } = [];
    }
}
