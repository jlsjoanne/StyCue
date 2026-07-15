using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Posts;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 貼文 API
    /// </summary>
    /// <remarks>
    /// 提供分享文與提問文的詳情查詢、建立、完整更新與刪除功能。
    /// 貼文圖片需先透過圖片上傳 API 取得 imageId，再由建立或更新貼文 API 綁定。
    /// 刪除採 soft delete，不會硬刪資料。
    /// </remarks>
    [Route("api/posts")]
    [ApiController]
    [Tags("Posts")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }


        /// <summary>
        /// 取得貼文詳情
        /// </summary>
        /// <remarks>
        /// 可未登入查詢。
        /// 若使用者已登入，回應會包含目前使用者是否為貼文作者，以及是否可編輯、可刪除、已按讚、已收藏。
        /// 回應包含作者摘要、圖片、標籤、按讚數、留言數與收藏數。
        /// </remarks>
        /// <param name="postId">貼文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>貼文詳情</returns>
        /// <response code="200">成功取得貼文詳情。</response>
        /// <response code="400">貼文 ID 不合法。</response>
        /// <response code="404">找不到指定的貼文。</response>
        [HttpGet("{postId:int}")]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPost(int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();

            var result = await _postService.GetPostAsync(userId, postId, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// 建立貼文
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 支援 share 分享文與 question 提問文。
        /// 圖片需先透過 POST /api/images/posts 上傳取得 imageId，且圖片必須屬於目前登入使用者、用途為 Post、尚未刪除且尚未綁定其他內容。
        /// tagIds 需為已存在的標籤 ID。
        /// 建立成功後會回傳最新貼文詳情。
        /// </remarks>
        /// <param name="request">建立貼文請求，包含標題、內容、貼文類型、圖片 ID 與標籤 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>建立完成後的貼文詳情</returns>
        /// <response code="201">貼文建立成功。</response>
        /// <response code="400">請求內容驗證失敗，或圖片、標籤資料不符合規則。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">沒有權限使用部分圖片。</response>
        /// <response code="404">找不到指定圖片。</response>
        /// <response code="409">圖片已被其他內容使用。</response>
        /// <response code="500">貼文建立後無法取得詳情。</response>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreatePost([FromBody] PostRequest request ,CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _postService.CreateAsync(userId, request, cancellationToken);

            if(!result.Success)
            {
                return ToActionResult(result);
            }

            return CreatedAtAction(nameof(GetPost), new { postId = result.Data!.PostId }, result);
        }


        /// <summary>
        /// 更新貼文
        /// </summary>
        /// <remarks>
        /// 需登入後使用，且只有貼文作者可以更新。
        /// PUT 採完整更新語意，request 的 title、content、postType、imageIds 與 tagIds 代表更新後的完整貼文內容。
        /// imageIds 會取代原本綁定的貼文圖片清單；未包含的既有圖片會從此貼文解除綁定。
        /// tagIds 會取代原本綁定的標籤清單。
        /// 更新成功後會回傳最新貼文詳情。
        /// </remarks>
        /// <param name="postId">貼文 ID</param>
        /// <param name="request">更新貼文請求，包含更新後完整的標題、內容、貼文類型、圖片 ID 與標籤 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>更新完成後的貼文詳情</returns>
        /// <response code="200">貼文更新成功。</response>
        /// <response code="400">請求內容驗證失敗，或貼文、圖片、標籤資料不符合規則。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">目前使用者不是貼文作者，或沒有權限使用部分圖片。</response>
        /// <response code="404">找不到指定貼文或圖片。</response>
        /// <response code="409">圖片已被其他內容使用。</response>
        /// <response code="500">貼文更新後無法取得詳情。</response>
        [Authorize]
        [HttpPut("{postId:int}")]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<PostDetailResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePost(int postId, [FromBody] PostRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _postService.UpdateAsync(userId, postId, request, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 刪除貼文
        /// </summary>
        /// <remarks>
        /// 需登入後使用，且只有貼文作者可以刪除。
        /// 此 API 採 soft delete，只標記刪除時間，不會硬刪除貼文資料。
        /// 刪除後貼文詳情與列表查詢不應再顯示此貼文。
        /// </remarks>
        /// <param name="postId">貼文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>貼文刪除結果</returns>
        /// <response code="200">貼文刪除成功。</response>
        /// <response code="400">貼文 ID 不合法。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">目前使用者不是貼文作者。</response>
        /// <response code="404">找不到指定的貼文。</response>
        [Authorize]
        [HttpDelete("{postId:int}")]
        [ProducesResponseType(typeof(ApiResponse<PostDeleteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PostDeleteResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PostDeleteResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<PostDeleteResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePost(int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _postService.DeleteAsync(userId, postId, cancellationToken);

            return ToActionResult(result);
        }

        // private method
        private IActionResult ToActionResult<T> (ApiResponse<T> result)
        {
            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "POST_NOT_FOUND" or
                "IMAGE_NOT_FOUND" => NotFound(result),

                "POST_NOT_OWNER" or
                "IMAGE_NOT_OWNER" => StatusCode(StatusCodes.Status403Forbidden, result),

                "IMAGE_ALREADY_BOUND" => Conflict(result),

                "POST_DETAIL_NOT_FOUND_AFTER_CREATE" or
                "POST_DETAIL_NOT_FOUND_AFTER_UPDATE" => StatusCode(StatusCodes.Status500InternalServerError, result),

                _ => BadRequest(result)
            };
        }
    }
}
