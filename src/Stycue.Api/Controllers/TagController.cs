using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 標籤API
    /// </summary>
    [Route("api/tags")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        /// <summary>
        /// 查詢標籤
        /// </summary>
        /// <remarks>
        /// 支援依關鍵字搜尋、熱門標籤、目前登入使用者常用標籤。
        /// Search 與 Popular 可匿名查詢；MyFrequent 需要帶 JWT，未登入會回傳 401。
        /// </remarks>
        /// <param name="request">標籤查詢條件，包含 keyword、tagCategory、source、limit。</param>
        /// <param name="cancellationToken">Request 取消通知。</param>
        /// <returns>符合條件的標籤清單。</returns>
        /// <response code="200">查詢成功。</response>
        /// <response code="400">查詢來源或標籤分類不合法。</response>
        /// <response code="401">查詢 MyFrequent 但未登入。</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTags([FromQuery] TagQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();
            var result = await _tagService.GetTagsAsync(userId, request, cancellationToken);

            if(!result.Success)
            {
                if(result.ErrorCode == "LOGIN_REQUIRED")
                {
                    return Unauthorized(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// 建立不存在的標籤，或回傳既有標籤
        /// </summary>
        /// <remarks>
        /// 前端可一次送出多個標籤。後端會先正規化名稱，再依正規化後的名稱判斷是否已存在資料庫。
        /// 已存在的標籤會直接回傳；不存在的標籤會建立後回傳。
        /// </remarks>
        /// <param name="request">要建立或取得的標籤清單。</param>
        /// <param name="cancellationToken">Request 取消通知。</param>
        /// <returns>建立或取得後的標籤清單。</returns>
        /// <response code="200">標籤建立或取得成功。</response>
        /// <response code="400">未提供標籤、標籤名稱空白，或標籤分類不合法。</response>
        /// <response code="401">未登入或登入資訊無效。</response>
        /// <response code="409">同名標籤已存在於不同分類。</response>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateOrGetTags(CreateTagRequest request, CancellationToken cancellationToken)
        {
            var result = await _tagService.CreateOrGetAsync(request, cancellationToken);

            if(!result.Success)
            {
                if( result.ErrorCode == "TAG_CATEGORY_CONFLICT")
                {
                    return Conflict(result);
                }

                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
