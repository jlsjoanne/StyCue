using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Likes;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 按讚 API
    /// </summary>
    /// <remarks>
    /// 提供留言與委託文的按讚、取消按讚功能
    /// 按讚與取消按讚皆採冪等設計：已按讚時重複按讚不會建立重複資料；未按讚時取消按讚不會回傳錯誤
    /// </remarks>
    [Authorize]
    [Route("api")]
    [ApiController]
    public class LikeController : ControllerBase
    {
        private readonly ILikeService _likeService;

        public LikeController(ILikeService likeService)
        {
            _likeService = likeService;
        }

        /// <summary>
        /// 對留言按讚
        /// </summary>
        /// <remarks>
        /// 需登入後使用
        /// 若目前使用者已對此留言按讚，會直接回傳目前按讚狀態與按讚數，不會建立重複按讚資料
        /// </remarks>
        /// <param name="commentId">留言 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>留言按讚後的狀態與目前按讚數</returns>
        /// <response code="200">留言按讚成功，或已按讚並回傳目前狀態。</response>
        /// <response code="400">留言 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的留言。</response>
        [HttpPost("comments/{commentId:int}/likes")]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LikeComment(int commentId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _likeService.LikeCommentAsync(userId, commentId, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 取消留言按讚
        /// </summary>
        /// <remarks>
        /// 需登入後使用
        /// 若目前使用者尚未對此留言按讚，會直接回傳目前未按讚狀態與按讚數，不會回傳錯誤
        /// </remarks>
        /// <param name="commentId">留言 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>取消留言按讚後的狀態與目前按讚數</returns>
        /// <response code="200">取消留言按讚成功，或原本未按讚並回傳目前狀態。</response>
        /// <response code="400">留言 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的留言。</response>
        [HttpDelete("comments/{commentId:int}/likes")]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnlikeComment(int commentId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _likeService.UnlikeCommentAsync(userId, commentId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 對委託文按讚
        /// </summary>
        /// <remarks>
        /// 需登入後使用
        /// 若目前使用者已對此委託文按讚，會直接回傳目前按讚狀態與按讚數，不會建立重複按讚資料
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>委託文按讚後的狀態與目前按讚數</returns>
        /// <response code="200">委託文按讚成功，或已按讚並回傳目前狀態。</response>
        /// <response code="400">委託文 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的委託文。</response>
        [HttpPost("commissions/{commissionId:int}/likes")]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LikeCommission(int commissionId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _likeService.LikeCommissionAsync(userId, commissionId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取消委託文按讚
        /// </summary>
        /// <remarks>
        /// 需登入後使用
        /// 若目前使用者尚未對此委託文按讚，會直接回傳目前未按讚狀態與按讚數，不會回傳錯誤
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>取消委託文按讚後的狀態與目前按讚數</returns>
        /// <response code="200">取消委託文按讚成功，或原本未按讚並回傳目前狀態。</response>
        /// <response code="400">委託文 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的委託文。</response>
        [HttpDelete("commissions/{commissionId:int}/likes")]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<LikeResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnlikeCommission(int commissionId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _likeService.UnlikeCommissionAsync(userId, commissionId, cancellationToken);
            return ToActionResult(result);
        }

        // private methods
        private IActionResult ToActionResult(ApiResponse<LikeResponse> result)
        {
            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "COMMENT_NOT_FOUND" or
                "COMMISSION_NOT_FOUND" => NotFound(result),

                _ => BadRequest(result)
            };
        }
    }
}
