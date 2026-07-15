namespace Stycue.Api.DTOs.Posts
{
    /// <summary>
    /// 貼文刪除回應。
    /// </summary>
    public class PostDeleteResponse
    {
        /// <summary>
        /// 已刪除的貼文 ID。
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 刪除時間。
        /// </summary>
        public DateTime DeletedAt { get; set; }
    }
}
