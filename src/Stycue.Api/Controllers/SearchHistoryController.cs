using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.SearchHistory;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 搜尋紀錄 API
    /// </summary>
    /// <remarks>
    /// 提供目前登入使用者的最近搜尋紀錄查詢與清除功能。
    ///
    /// 搜尋紀錄僅儲存 keyword 與搜尋時間，不儲存 SearchHit 或搜尋結果內容；
    /// 貼文與委託文可能被編輯、刪除或關閉，使用者點擊歷史 keyword 時，
    /// 前端應重新呼叫 GET /api/search 取得最新結果。
    ///
    /// 同一使用者的相同 keyword 僅保留一筆；再次成功搜尋時會更新 SearchedAt。
    /// 每位使用者最多保留最近 10 筆紀錄。
    /// </remarks>
    [Authorize]
    [Route("api/users/me/search-history")]
    [ApiController]
    [Tags("SearchHistory")]
    public class SearchHistoryController : ControllerBase
    {
        private readonly ISearchHistoryService _searchHistoryService;

        public SearchHistoryController(ISearchHistoryService searchHistoryService)
        {
            _searchHistoryService = searchHistoryService;
        }

        /// <summary>
        /// 取得目前使用者的最近搜尋紀錄
        /// </summary>
        /// <remarks>
        ///  依 SearchedAt 由新到舊回傳目前登入使用者的搜尋紀錄。
        /// 未帶 limit 時預設回傳最近 5 筆；limit 小於 1 時使用預設值，
        /// 超過上限時由後端限制為最大筆數。
        ///
        /// 沒有搜尋紀錄時回傳成功與空清單。
        /// </remarks>
        /// <param name="request">搜尋紀錄查詢條件，包含 limit</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的最近搜尋紀錄</returns>
        /// <response code="200">成功取得搜尋紀錄；沒有紀錄時回傳空清單。</response>
        /// <response code="400">目前使用者 ID 或查詢條件不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="500">取得搜尋紀錄時發生未預期錯誤。</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<SearchHistoryResponse>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<SearchHistoryResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<List<SearchHistoryResponse>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSearchHistory(
            [FromQuery] SearchHistoryQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _searchHistoryService.GetMyHistoryAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 清除目前使用者的全部搜尋紀錄
        /// </summary>
        /// <remarks>
        /// 僅清除目前登入使用者的搜尋紀錄，不會影響其他使用者資料、
        /// SearchDocument、FashionSearchDictionary 或貼文／委託文內容。
        ///
        /// 此操作採冪等設計：即使目前沒有任何搜尋紀錄，仍回傳成功。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>清除結果與實際刪除筆數</returns>
        /// <response code="200">搜尋紀錄清除成功；可能刪除 0 筆。</response>
        /// <response code="400">目前使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="500">清除搜尋紀錄時發生未預期錯誤。</response>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSearchHistory(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _searchHistoryService.ClearMyHistoryAsync(userId, cancellationToken);
            return ToActionResult(result);
        }

        private IActionResult ToActionResult<T>(ApiResponse<T> result)
        {
            if(result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "SEARCH_HISTORY_QUERY_FAILED" or
                "SEARCH_HISTORY_CLEAR_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, result),

                "INVALID_USER_ID" => BadRequest(result),

                _ => BadRequest(result)
            };
        }
    }
}
