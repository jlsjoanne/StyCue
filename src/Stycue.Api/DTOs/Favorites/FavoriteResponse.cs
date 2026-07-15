namespace Stycue.Api.DTOs.Favorites
{
    /// <summary>
    /// 收藏或取消收藏後的回應資料
    /// </summary>
    public class FavoriteResponse
    {
        /// <summary>
        /// 收藏目標類型。
        /// post、commission
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// 收藏目標 Id。
        /// 依 TargetType 對應 PostId 或 CommissionId。
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// 目前登入使用者是否已收藏此目標。
        /// 收藏成功時為 true，取消收藏成功時為 false。
        /// </summary>
        public bool IsFavorited { get; set; }

        /// <summary>
        /// 此目標目前的收藏總數
        /// </summary>
        public int FavoriteCount { get; set; }
    }
}
