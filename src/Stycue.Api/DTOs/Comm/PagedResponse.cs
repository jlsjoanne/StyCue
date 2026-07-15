namespace Stycue.Api.DTOs.Comm
{
    /// <summary>
    /// 分頁查詢回應 DTO。
    /// </summary>
    /// <typeparam name="T">分頁項目的資料型別。</typeparam>
    /// <remarks>
    /// 用於回傳列表資料與分頁資訊。
    /// 前端可依據 Page、PageSize、TotalCount、TotalPages 判斷是否還有下一頁資料。
    /// </remarks>
    public class PagedResponse<T>
    {
        /// <summary>
        /// 當前頁面的資料清單。
        /// </summary>
        public IReadOnlyList<T> Items { get; set; } = [];

        /// <summary>
        /// 目前頁碼，從 1 開始。
        /// </summary>
        /// <example>1</example>
        public int Page { get; set; }

        /// <summary>
        /// 每頁資料筆數。
        /// </summary>
        /// <example>20</example>
        public int PageSize { get; set; }

        /// <summary>
        /// 符合查詢條件的總資料筆數。
        /// </summary>
        /// <example>125</example>
        public int TotalCount { get; set; }

        /// <summary>
        /// 總頁數。
        /// </summary>
        /// <remarks>
        /// 若 TotalCount 為 0，則回傳 0。
        /// </remarks>
        /// <example>7</example>
        public int TotalPages { get; set; }
    }
}
