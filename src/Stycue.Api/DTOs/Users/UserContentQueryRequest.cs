using Stycue.Api.DTOs.Comm;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Users
{
    /// <summary>
    /// 使用者發表內容列表查詢條件。
    /// </summary>
    public class UserContentQueryRequest : PagedQueryRequest
    {
        /// <summary>
        /// 內容類型篩選。
        /// all = 全部；postShare = 分享貼文；postAsk = 提問貼文；commission = 委託文。
        /// 預設回傳全部類型。
        /// </summary>
        public HomepageFilter Filter { get; set; } = HomepageFilter.All;
    }
}
