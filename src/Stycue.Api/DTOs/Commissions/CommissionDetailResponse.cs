using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Enums;
using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文詳情回應
    /// </summary>
    public class CommissionDetailResponse
    {
        /// <summary>
        /// 委託文Id
        /// </summary>
        public int CommissionId { get; set; }

        /// <summary>
        /// 發表者摘要
        /// </summary>
        public UserSummaryResponse Author { get; set; } = new();
        
        /// <summary>
        /// 委託文標題
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 委託文內容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 委託文狀態 => {委託進行中=1, 已過期=2, 提前關閉=3, 積分已發放=4, 流標=5}
        /// </summary>
        public CommissionStatus Status { get; set; }

        /// <summary>
        /// 身高，單位cm
        /// </summary>
        public decimal Height { get; set; }

        /// <summary>
        /// 體重，單位kg
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 年齡
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 預算描述
        /// </summary>
        public string Budget { get; set; } = string.Empty;

        /// <summary>
        /// 委託懸賞積分
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// 最佳留言Id
        /// 該委託最後選中的最佳留言是哪一則
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AwardedCommentId { get; set; }

        /// <summary>
        /// 被選為最佳留言的使用者實際獲得積分。
        /// 已扣除委託手續積分；尚未發放最佳留言獎勵時不回傳。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RewardPoints { get; set; }

        /// <summary>
        /// 最佳留言積分發放時間
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? AwardedAt { get; set; }

        /// <summary>
        /// 最高讚留言發放積分時間
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RewardSettledAt { get; set; }

        /// <summary>
        /// 重新發表委託次數
        /// </summary>
        public int RepostCount { get; set; }

        /// <summary>
        /// 委託文發表時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 委託文更新時間
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 委託文過期時間
        /// </summary>
        public DateTime ExpiredAt { get; set; }

        /// <summary>
        /// 委託文關閉時間
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// 目前登入使用者是不是這篇委託文的建立者
        /// IsOwner == true => 可以關閉委託、重新發表、加碼積分、選擇最佳留言操作
        /// IsOwner == false => 不顯示委託人專用操作
        /// </summary>
        public bool IsOwner { get; set; }

        /// <summary>
        /// 是否過期
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// 是否可重新發表
        /// </summary>
        public bool CanRepost { get; set; }

        /// <summary>
        /// 是否可加碼
        /// </summary>
        public bool CanBoost { get; set; }

        /// <summary>
        /// 是否可以選擇最佳留言
        /// </summary>
        public bool CanSelectBestComment { get; set; }

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
        /// 委託文圖片清單
        /// </summary>
        public IReadOnlyList<ImageResponse> Images { get; set; } = [];

        /// <summary>
        /// 委託文標籤清單
        /// </summary>
        public IReadOnlyList<TagResponse> Tags { get; set; } = [];

        /// <summary>
        /// 委託文重新發表內容
        /// </summary>
        public IReadOnlyList<CommissionRepostResponse> Reposts { get; set; } = [];
    }
}
