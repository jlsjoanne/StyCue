using Stycue.Api.DTOs.Comm;
using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Search
{
    public class SearchQueryRequest : PagedQueryRequest
    {
        [Required(ErrorMessage = "請輸入搜尋關鍵字")]
        [StringLength(100, ErrorMessage = "搜尋關鍵字不可超過 100 個字元")]
        public string Keyword { get; set; } = string.Empty;
    }
}
