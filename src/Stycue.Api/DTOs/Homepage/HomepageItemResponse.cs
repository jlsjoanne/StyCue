using System.Text.Json.Serialization;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Homepage
{
    /// <summary>
    /// 首頁列表單筆項目
    /// </summary>
    public class HomepageItemResponse
    {
        /// <summary>
        /// 首頁項目類型，判斷點擊後進入分享貼文、提問貼文或委託詳情
        /// API 對外值：postShare、postAsk、commission
        /// </summary>
        public HomepageItemType ItemType { get; set; }

        /// <summary>
        /// 對應 Post.Id 或 Commission.Id
        /// </summary>
        public int ItemId { get; set; }
        
        /// <summary>
        /// 發表者摘要
        /// </summary>
        public UserSummaryResponse Author { get; set; } = new();
        
        /// <summary>
        /// 標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 內容預覽文字，由後端依固定長度裁切後回傳
        /// 首頁列表不回傳完整 Content，完整內容使用GET /api/posts/{postId}或 /api/commissions/{commissionId}查詢
        /// </summary>
        public string ContentPreview { get; set; } = string.Empty;

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 留言數
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 按讚數
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 目前登入使用者是否已按讚。
        /// 未登入時不回傳此欄位。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsLiked { get; set; }

        /// <summary>
        /// 收藏數
        /// </summary>
        public int FavoriteCount { get; set; }

        /// <summary>
        /// 目前登入使用者是否已收藏。
        /// 未登入時不回傳此欄位。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsFavorited { get; set; }

        /// <summary>
        /// 圖片列表
        /// </summary>
        public IReadOnlyList<ImageResponse> Images { get; set; } = [];

        /// <summary>
        /// 標籤列表
        /// </summary>
        public IReadOnlyList<TagResponse> Tags { get; set; } = [];

        /// <summary>
        /// 委託文狀態，僅委託文會回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CommissionStatus? CommissionStatus { get; set; }

        /// <summary>
        /// 委託文目前懸賞積分，僅委託文會回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CommissionPoints { get; set; }

        /// <summary>
        /// 委託文到期時間，僅委託文會回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ExpiredAt { get; set; }

        /// <summary>
        /// 貼文類型，僅分享/提問文會回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PostType? PostType { get; set; }
    }
}
