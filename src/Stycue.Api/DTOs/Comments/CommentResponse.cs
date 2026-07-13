using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Comments
{
    /// <summary>
    /// 留言Response
    /// </summary>
    public class CommentResponse
    {
        /// <summary>
        /// 留言 Id
        /// </summary>
        public int CommentId { get; set; }

        /// <summary>
        /// 留言作者摘要
        /// </summary>
        public UserSummaryResponse Author { get; set; } = new();

        /// <summary>
        /// 所屬貼文 Id，為委託文留言時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PostId { get; set; }

        /// <summary>
        /// 所屬委託文 Id，為貼文留言時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CommissionId { get; set; }

        /// <summary>
        /// 根留言 Id，留言為根留言時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ParentCommentId { get; set; }

        /// <summary>
        /// 留言內容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 留言建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 留言更新時間，未編輯時不回傳
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 目前登入使用者是否為留言作者
        /// </summary>
        public bool IsOwner { get; set; } = false;

        /// <summary>
        /// 是否可編輯留言
        /// </summary>
        public bool CanEdit { get; set; } = false;

        /// <summary>
        /// 是否可刪除留言
        /// </summary>
        public bool CanDelete { get; set; } = false;

        /// <summary>
        /// 留言按讚數
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 目前登入使用者是否已按讚
        /// </summary>
        public bool IsLiked { get; set; } = false;

        /// <summary>
        /// 留言圖片清單
        /// </summary>
        public IReadOnlyList<ImageResponse> Images { get; set; } = [];

        /// <summary>
        /// 回覆留言清單
        /// </summary>
        public IReadOnlyList<CommentResponse> Replies { get; set; } = [];
    }
}
