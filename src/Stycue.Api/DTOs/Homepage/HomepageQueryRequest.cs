using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.DTOs.Homepage
{
    /// <summary>
    /// 首頁列表查詢條件
    /// </summary>
    public class HomepageQueryRequest : PagedQueryRequest
    {
        /// <summary>
        /// 排序方式
        /// 可用值：latest、mostLikes、mostComments
        /// </summary>
        /// <example>mostLikes</example>
        public string SortBy { get; set; } = "mostLikes";

        /// <summary>
        /// 篩選類型
        /// 可用值：all、postShare、postAsk、commission
        /// </summary>
        /// <example>all</example>
        public string Filter { get; set; } = "all";
    }
}
