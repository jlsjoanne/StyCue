using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.Extensions;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 首頁列表 API
    /// </summary>
    /// <remarks>
    /// 提供首頁混合列表查詢，回傳分享文、提問文與委託文的列表資料。
    /// 目前第一階段已支援委託文列表；分享文與提問文尚未實作時，filter=postShare 或 filter=postAsk 會回傳成功且 items 為空集合。
    ///
    /// 可未登入查詢。
    /// 若使用者已登入，後續可依登入使用者補充個人化狀態欄位。
    ///
    /// 支援排序：
    /// - latest：依建立時間由新到舊排序。
    /// - mostLikes：第一層依按讚數由高到低排序，第二層依 UpdatedAt 由新到舊排序；若 UpdatedAt 為 null，則使用 CreatedAt。
    /// - mostComments：第一層依留言數由高到低排序，第二層依 UpdatedAt 由新到舊排序；若 UpdatedAt 為 null，則使用 CreatedAt。
    ///
    /// 支援篩選：
    /// - all：回傳所有首頁項目。目前會回傳委託文，Post 尚未實作前分享文與提問文為空。
    /// - commission：只回傳委託文。
    /// - postShare：只回傳分享文；目前 Post 尚未實作時回傳空集合。
    /// - postAsk：只回傳提問文；目前 Post 尚未實作時回傳空集合。
    ///
    /// 未帶 sortBy 時，預設使用 mostLikes。
    /// 未帶 filter 時，預設使用 all。
    /// page 小於 1 時會修正為 1；pageSize 小於 1 時會修正為預設值，超過上限時會修正為系統最大值。
    /// </remarks>
    [Route("api/homepage")]
    [ApiController]
    [Tags("Homepage")]
    public class HomepageController : ControllerBase
    {
        private readonly IHomepageService _homepageService;

        public HomepageController(IHomepageService homepageService)
        {
            _homepageService = homepageService;
        }

        /// <summary>
        /// 查詢首頁列表
        /// </summary>
        /// <remarks>
        /// 依指定排序、篩選與分頁條件查詢首頁列表。
        /// 首頁列表項目會以 itemType 標示資料類型，前端可依 itemType 決定導向委託文詳情或貼文詳情。
        /// </remarks>
        /// <param name="request">首頁列表查詢條件，包含 sortBy、filter、page 與 pageSize</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>首頁列表分頁結果</returns>
        /// <response code="200">首頁列表查詢成功。</response>
        /// <response code="400">查詢條件不合法，例如 sortBy 或 filter 不支援。</response>
        /// <response code="500">首頁列表查詢時發生未預期錯誤。</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Index(HomepageQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();

            var result = await _homepageService.GetHomepageAsync(userId, request, cancellationToken);

            return ToActionResult(result);
        }

        private IActionResult ToActionResult(ApiResponse<PagedResponse<HomepageItemResponse>> result)
        {
            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "HOMEPAGE_QUERY_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, result),
                _ => BadRequest(result)
            };
        }
    }
}
