namespace Stycue.Api.DTOs.Comm
{
    /// <summary>
    /// 分頁查詢基底 DTO。
    /// </summary>
    /// <remarks>
    /// 提供列表查詢共用的分頁參數。各功能可繼承此類別並加入自己的查詢條件，
    /// 例如關鍵字、狀態、分類或排序條件。
    /// </remarks>
    public class PagedQueryRequest
    {
        /// <summary>
        /// 頁碼，從 1 開始。
        /// </summary>
        /// <remarks>
        /// 若傳入小於 1 的值，則修正為 1。
        /// </remarks>
        /// <example>1</example>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每頁資料筆數。
        /// </summary>
        /// <remarks>
        /// 建議最小 1、最大 50，避免一次查詢過多資料。
        /// </remarks>
        /// <example>20</example>
        public int PageSize { get; set; } = 20;
    }
}
