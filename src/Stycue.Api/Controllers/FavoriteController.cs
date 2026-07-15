using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Favorites;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 收藏 API
    /// </summary>
    /// <remarks>
    /// 提供貼文與委託文的收藏、取消收藏功能。
    /// 收藏與取消收藏皆採冪等設計：已收藏時重複收藏不會建立重複資料；未收藏時取消收藏不會回傳錯誤。
    /// 已刪除的貼文不可收藏或取消收藏；已提前關閉的委託文不可收藏或取消收藏。
    /// </remarks>
    [Authorize]
    [Route("api")]
    [ApiController]
    [Tags("Favorites")]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoriteController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        /// <summary>
        /// 收藏委託文
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 若目前使用者已收藏此委託文，會直接回傳目前收藏狀態與收藏數，不會建立重複收藏資料。
        /// 已提前關閉的委託文不可收藏。
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>委託文收藏後的狀態與目前收藏數</returns>
        /// <response code="200">委託文收藏成功，或已收藏並回傳目前狀態。</response>
        /// <response code="400">委託文 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的委託文。</response>
        [HttpPost("commissions/{commissionId:int}/favorites")]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FavoriteCommission(int commissionId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _favoriteService.FavoriteCommissionAsync(userId, commissionId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 取消收藏委託文
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 若目前使用者尚未收藏此委託文，會直接回傳目前未收藏狀態與收藏數，不會回傳錯誤。
        /// 已提前關閉的委託文不可取消收藏。
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>取消收藏委託文後的狀態與目前收藏數</returns>
        /// <response code="200">取消收藏委託文成功，或原本未收藏並回傳目前狀態。</response>
        /// <response code="400">委託文 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的委託文。</response>
        [HttpDelete("commissions/{commissionId:int}/favorites")]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnfavoriteCommission(int commissionId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _favoriteService.UnfavoriteCommissionAsync(userId, commissionId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 收藏分享/提問貼文
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 若目前使用者已收藏此貼文，會直接回傳目前收藏狀態與收藏數，不會建立重複收藏資料。
        /// 已刪除的貼文不可收藏。
        /// </remarks>
        /// <param name="postId">貼文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>貼文收藏後的狀態與目前收藏數</returns>
        /// <response code="200">貼文收藏成功，或已收藏並回傳目前狀態。</response>
        /// <response code="400">貼文 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的貼文。</response>
        [HttpPost("posts/{postId:int}/favorites")]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FavoritePost(int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _favoriteService.FavoritePostAsync(userId, postId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取消收藏分享/提問貼文
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 若目前使用者尚未收藏此貼文，會直接回傳目前未收藏狀態與收藏數，不會回傳錯誤。
        /// 已刪除的貼文不可取消收藏。
        /// </remarks>
        /// <param name="postId">貼文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>取消收藏貼文後的狀態與目前收藏數</returns>
        /// <response code="200">取消收藏貼文成功，或原本未收藏並回傳目前狀態。</response>
        /// <response code="400">貼文 ID 或使用者資訊不合法。</response>
        /// <response code="401">尚未登入，或登入資訊無效。</response>
        /// <response code="404">找不到指定的貼文。</response>
        [HttpDelete("posts/{postId:int}/favorites")]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<FavoriteResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnfavoritePost(int postId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _favoriteService.UnfavoritePostAsync(userId, postId, cancellationToken);
            return ToActionResult(result);
        }

        // private helper
        private IActionResult ToActionResult(ApiResponse<FavoriteResponse> result)
        {
            if(result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "POST_NOT_FOUND" or
                "COMMISSION_NOT_FOUND" => NotFound(result),

                "INVALID_USER_ID" => Unauthorized(result),

                _ => BadRequest(result)
            };
        }
    }
}
