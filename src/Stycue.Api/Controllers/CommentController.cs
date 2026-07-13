using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Comments;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 留言 API
    /// </summary>
    /// <remarks>
    /// 提供委託文留言列表、建立根留言、回覆留言、編輯留言與刪除留言功能
    /// 目前支援委託文留言
    /// </remarks>
    [Route("api")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }


        /// <summary>
        /// 取得委託文留言列表
        /// </summary>
        /// <remarks>
        /// 可未登入查詢。若使用者已登入，回應會包含是否為留言作者、是否可編輯、是否可刪除與是否已按讚
        /// 回傳根留言列表，並包含第一層回覆留言
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>委託文留言列表</returns>
        /// <response code="200">成功取得留言列表。</response>
        /// <response code="400">委託文 ID 不合法，或留言目標不合法。</response>
        /// <response code="404">找不到指定的委託文。</response>
        [HttpGet("commissions/{commissionId:int}/comments")]
        [ProducesResponseType(typeof(ApiResponse<List<CommentResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<CommentResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<List<CommentResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCommissionComment(
            int commissionId,CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();

            var result = await _commentService.GetCommissionCommentsAsync(userId, commissionId, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// 建立委託文根留言
        /// </summary>
        /// <remarks>
        /// 需登入後使用，委託建立者不可對自己的委託建立根留言
        /// 歷史委託仍可討論，因此不因委託狀態或到期時間阻擋新增留言
        /// 圖片需先透過留言圖片上傳 API 取得 imageId，再由此 API 綁定到留言
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="request">留言內容與圖片 ID 清單</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>建立完成的留言資料</returns>
        /// <response code="200">留言建立成功。</response>
        /// <response code="400">請求內容驗證失敗，或圖片資料不符合規則。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="403">目前使用者不可對此委託建立根留言，或沒有權限使用部分圖片。</response>
        /// <response code="404">找不到指定的委託文或圖片。</response>
        /// <response code="409">圖片已被其他內容使用。</response>
        /// <response code="500">留言建立後無法取得回應資料。</response>
        [Authorize]
        [HttpPost("commissions/{commissionId:int}/comments")]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCommissionComment(
            int commissionId, [FromBody] UpsertCommentRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commentService.CreateForCommissionAsync(
                userId, commissionId, request, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// 回覆留言
        /// </summary>
        /// <remarks>
        /// 需登入後使用，只能回覆根留言，不允許回覆留言下的回覆
        /// 回覆會繼承父留言所屬的貼文或委託文
        /// </remarks>
        /// <param name="commentId">父留言 ID</param>
        /// <param name="request">回覆內容與圖片 ID 清單</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>建立完成的回覆留言資料</returns>
        /// <response code="200">回覆建立成功。</response>
        /// <response code="400">請求內容驗證失敗，或留言目標、圖片資料不符合規則。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="403">沒有權限使用部分圖片。</response>
        /// <response code="404">找不到指定的留言或圖片。</response>
        /// <response code="409">不允許回覆留言的回覆，或圖片已被其他內容使用。</response>
        /// <response code="500">留言建立後無法取得回應資料。</response>
        [Authorize]
        [HttpPost("comments/{commentId:int}/replies")]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReplyComment(
            int commentId, [FromBody] UpsertCommentRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commentService.ReplyAsync(userId, commentId, request, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 編輯留言
        /// </summary>
        /// <remarks>
        /// 需登入後使用，只有留言作者可以編輯留言
        /// request 的 imageIds 代表更新後保留或綁定的完整圖片清單；未包含的既有圖片會從此留言解除綁定
        /// </remarks>
        /// <param name="commentId">留言 ID</param>
        /// <param name="request">更新後的留言內容與圖片 ID 清單</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>更新完成的留言資料</returns>
        /// <response code="200">留言更新成功。</response>
        /// <response code="400">請求內容驗證失敗，或圖片資料不符合規則。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="403">目前使用者不是留言作者，或沒有權限使用部分圖片。</response>
        /// <response code="404">找不到指定的留言或圖片。</response>
        /// <response code="409">圖片已被其他內容使用。</response>
        /// <response code="500">留言更新後無法取得回應資料。</response>
        [Authorize]
        [HttpPut("comments/{commentId:int}")]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateComment(
            int commentId, [FromBody] UpsertCommentRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commentService.UpdateAsync(userId, commentId, request, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// 刪除留言
        /// </summary>
        /// <remarks>
        /// 需登入後使用，只有留言作者可以刪除留言
        /// 此 API 採 soft delete，只標記刪除時間，不會硬刪除留言資料
        /// </remarks>
        /// <param name="commentId">留言 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>留言刪除結果</returns>
        /// <response code="200">留言已刪除。</response>
        /// <response code="400">留言 ID 不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="403">目前使用者不是留言作者。</response>
        /// <response code="404">找不到指定的留言。</response>
        [Authorize]
        [HttpDelete("comments/{commentId:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteComment(int commentId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commentService.DeleteAsync(userId, commentId, cancellationToken);

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
                "COMMENT_RESPONSE_NOT_FOUND" =>
                    StatusCode(StatusCodes.Status500InternalServerError, result),

                "COMMISSION_NOT_FOUND" or
                "COMMENT_NOT_FOUND" or
                "IMAGE_NOT_FOUND" =>
                    NotFound(result),

                "COMMENT_NOT_OWNER" or
                "COMMISSION_OWNER_CANNOT_COMMENT" or
                "IMAGE_NOT_OWNER" =>
                    StatusCode(StatusCodes.Status403Forbidden, result),

                "REPLY_TO_REPLY_NOT_ALLOWED" or
                "IMAGE_ALREADY_BOUND" => Conflict(result),

                "INVALID_USER_ID" => Unauthorized(result),

                _ => BadRequest(result)
            };
        }
    }
}
