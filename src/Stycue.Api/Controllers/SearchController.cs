using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Search;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 關鍵字搜尋 API
    /// </summary>
    /// <remarks>
    /// 搜尋分享貼文、提問貼文與委託文的標題、內容及關聯 Tag 名稱。
    ///
    /// 可未登入查詢；若使用者已登入，回應會一併包含目前使用者的
    /// isLiked、isFavorited 與 author.isFollowing 狀態。
    ///
    /// 搜尋結果固定回傳混合內容，前端可依 itemType 決定導向貼文或委託文詳情。
    /// 回應沿用 HomepageItemResponse；前端可依畫面需求顯示 title、contentPreview、
    /// images 或其他既有欄位。
    ///
    /// 第一版由 MvpSearchCandidateProvider 對最多 500 筆可見 SearchDocument
    /// 進行原始 keyword 與受控同義詞擴展的加權排名。
    ///
    /// 已 soft delete 的貼文與已關閉的委託文不會出現在搜尋結果。
    /// page 小於 1 時會修正為 1；pageSize 小於 1 時使用預設值，最大為 50。
    /// </remarks>
    [Route("api/search")]
    [ApiController]
    [Tags("Search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// 搜尋貼文與委託文
        /// </summary>
        /// <remarks>
        /// 依 keyword 查詢 Post Share、Post Ask 與 Commission 的混合結果。
        /// 搜尋結果依相關性 Rank 排序；同分時依最後更新時間、內容類型與內容 ID
        /// 進行穩定排序。
        ///
        /// keyword 不可為空白，且最多 100 個字元。系統會以 FashionQueryExpander
        /// 進行受控的穿搭領域同義詞擴展。
        /// </remarks>
        /// <param name="request">搜尋查詢條件，包含 keyword、page 與 pageSize</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>搜尋結果的分頁列表</returns>
        /// <response code="200">搜尋成功；沒有符合結果時回傳空 items。</response>
        /// <response code="400">搜尋 keyword、分頁條件或目前使用者 ID 不合法。</response>
        /// <response code="500">搜尋處理時發生未預期錯誤。</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromQuery] SearchQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();

            var result = await _searchService.SearchAsync(userId, request, cancellationToken);

            return ToActionResult(result);
        }

        private IActionResult ToActionResult(ApiResponse<PagedResponse<HomepageItemResponse>> result)
        {
            if(result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "SEARCH_FAILED" => StatusCode(
                    StatusCodes.Status500InternalServerError, result),

                "INVALID_USER_ID" or
                "VALIDATION_FAILED" => BadRequest(result),

                _ => BadRequest(result)
            };
        }
    }
}
