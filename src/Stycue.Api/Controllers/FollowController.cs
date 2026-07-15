using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Follow;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 追蹤 API
    /// </summary>
    /// <remarks>
    /// 提供目前登入使用者追蹤與取消追蹤其他使用者的功能。
    /// 追蹤與取消追蹤皆採冪等設計：已追蹤時重複追蹤不會建立重複資料；未追蹤時取消追蹤不會回傳錯誤。
    /// 使用者不可追蹤自己，且不可追蹤已停用或不存在的使用者。
    /// </remarks>
    [Authorize]
    [Route("api/users/me/follow")]
    [ApiController]
    [Tags("Follows")]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        /// <summary>
        /// 追蹤使用者
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 若目前使用者已追蹤目標使用者，會直接回傳目前追蹤狀態與目標使用者粉絲數，不會建立重複追蹤資料。
        /// 使用者不可追蹤自己。
        /// </remarks>
        /// <param name="targetUserId">被追蹤的目標使用者 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>追蹤後的狀態與目標使用者目前粉絲數</returns>
        /// <response code="200">追蹤使用者成功，或已追蹤並回傳目前狀態。</response>
        /// <response code="400">目標使用者 ID 不合法，或不可追蹤自己。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的使用者。</response>
        [HttpPost("{targetUserId:int}")]
        [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FollowUser(int targetUserId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _followService.FollowUserAsync(userId, targetUserId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取消追蹤使用者
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 若目前使用者尚未追蹤目標使用者，會直接回傳目前未追蹤狀態與目標使用者粉絲數，不會回傳錯誤。
        /// 使用者不可對自己執行取消追蹤。
        /// </remarks>
        /// <param name="targetUserId">取消追蹤的目標使用者 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>取消追蹤後的狀態與目標使用者目前粉絲數</returns>
        /// <response code="200">取消追蹤使用者成功，或原本未追蹤並回傳目前狀態。</response>
        /// <response code="400">目標使用者 ID 不合法，或不可對自己執行取消追蹤。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的使用者。</response>
        [HttpDelete("{targetUserId:int}")]
        [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnfollowUser(int targetUserId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _followService.UnfollowUserAsync(userId, targetUserId, cancellationToken);
            return ToActionResult(result);
        }


        // private method
        private IActionResult ToActionResult(ApiResponse<FollowResponse> result)
        {
            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "TARGET_USER_NOT_FOUND" => NotFound(result),

                "INVALID_USER_ID" => Unauthorized(result),

                _ => BadRequest(result)
            };
        }
    }
}
