using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Stycue.Api.DTOs.Notifications;
using Microsoft.AspNetCore.Authorization;
using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 站內通知 API
    /// </summary>
    /// <remarks>
    /// 提供目前登入使用者的站內通知查詢、未讀數查詢，以及單筆或全部設為已讀功能。
    ///
    /// 通知只會回傳給其收件人；使用者無法透過本 API 查詢、修改或確認其他使用者的通知。
    /// 本期通知採持久化資料與前端輪詢／進入頁面時查詢，不提供即時推播或刪除通知功能。
    /// </remarks>
    [Authorize]
    [Route("api/notifications")]
    [ApiController]
    [Tags("Notification")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// 取得目前登入使用者的通知列表
        /// </summary>
        /// <remarks>
        /// 依通知建立時間由新到舊回傳分頁資料；相同建立時間時，以通知 ID 由大到小排序。
        /// 可使用 <c>unreadOnly=true</c> 僅取得未讀通知。
        ///
        /// 沒有通知或篩選後沒有符合資料時，仍回傳成功與空清單。
        /// 頁碼與每頁筆數會由後端正規化為允許範圍。
        /// </remarks>
        /// <param name="request">通知列表的分頁與未讀篩選條件</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的通知分頁結果</returns>
        /// <response code="200">成功取得通知列表；可能回傳空清單。</response>
        /// <response code="400">查詢參數無法繫結為預期型別時，由 API 自動驗證回傳。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="500">取得通知列表時發生未預期錯誤。</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyNotifications([FromQuery] NotificationQueryRequest? request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _notificationService.GetMyNotificationsAsync(
                userId, request ?? new NotificationQueryRequest(), cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 取得目前登入使用者的未讀通知數
        /// </summary>
        /// <remarks>
        /// 僅計算目前登入使用者尚未標記為已讀的通知。
        /// 沒有未讀通知時，成功回傳 <c>unreadCount = 0</c>。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的未讀通知數</returns>
        /// <response code="200">成功取得未讀通知數。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="500">取得未讀通知數時發生未預期錯誤。</response>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 將一筆通知設為已讀
        /// </summary>
        /// <remarks>
        /// 僅能更新目前登入使用者自己的通知。
        /// 此操作為冪等操作：通知已經是已讀狀態時，仍回傳成功與原本的已讀時間。
        ///
        /// 若指定通知不存在，或不屬於目前登入使用者，皆回傳 404，避免洩漏其他使用者的通知資訊。
        /// </remarks>
        /// <param name="notificationId">要設為已讀的通知 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>通知更新後的已讀狀態與已讀時間</returns>
        /// <response code="200">通知已設為已讀，或原本已是已讀。</response>
        /// <response code="400">通知 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到指定通知，或該通知不屬於目前登入使用者。</response>
        /// <response code="500">更新通知已讀狀態時發生未預期錯誤。</response>
        [HttpPatch("{notificationId:int}/read")]
        [ProducesResponseType(typeof(ApiResponse<NotificationReadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<NotificationReadResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<NotificationReadResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<NotificationReadResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(int notificationId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _notificationService.MarkAsReadAsync(userId, notificationId, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 將目前登入使用者的全部未讀通知設為已讀
        /// </summary>
        /// <remarks>
        /// 僅更新目前登入使用者自己的未讀通知，不會影響其他使用者資料。
        /// 此操作為冪等操作；沒有未讀通知時仍回傳成功，且 <c>updatedCount</c> 為 0。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>實際由未讀更新為已讀的通知筆數</returns>
        /// <response code="200">所有未讀通知已設為已讀；可能更新 0 筆。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="500">更新全部通知已讀狀態時發生未預期錯誤。</response>
        [HttpPatch("read-all")]
        [ProducesResponseType(typeof(ApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<MarkAllNotificationsReadResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);

            return ToActionResult(result);
        }

        // private methods
        private IActionResult ToActionResult<T>(ApiResponse<T> result)
        {
            if(result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "NOTIFICATION_NOT_FOUND" => NotFound(result),

                "INVALID_USER_ID" => Unauthorized(result),

                "NOTIFICATION_QUERY_FAILED" or
                "NOTIFICATION_UNREAD_COUNT_FAILED" or
                "NOTIFICATION_MARK_READ_FAILED" or
                "NOTIFICATION_MARK_ALL_READ_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, result),

                _ => BadRequest(result)
            };
        }
    }
}
