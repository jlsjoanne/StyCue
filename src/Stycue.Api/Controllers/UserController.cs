using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Follow;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Points;
using Stycue.Api.DTOs.Users;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 使用者資料、個人檔案、收藏內容與追蹤列表 API。
    /// </summary>
    /// <remarks>
    /// 提供目前登入使用者資料、公開使用者資料、個人資料更新、我的內容、收藏列表與追蹤/粉絲列表查詢。
    /// </remarks>
    [Route("api/users")]
    [ApiController]
    [Tags("Users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 取得目前登入使用者的帳號基本資料。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。回傳目前登入使用者的 Id、暱稱、Email、角色、Email 驗證狀態與建立/更新時間。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者資料</returns>
        /// <response code="200">取得目前登入使用者資料成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetCurrentUserAsync(userId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取得公開使用者個人資料。
        /// </summary>
        /// <remarks>
        /// 可未登入使用。若使用者已登入，會一併回傳目前登入使用者是否追蹤目標使用者。
        /// </remarks>
        /// <param name="targetUserId">目標使用者 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>公開使用者個人資料</returns>
        /// <response code="200">取得公開使用者資料成功。</response>
        /// <response code="400">目標使用者 ID 或目前使用者 ID 不合法。</response>
        /// <response code="404">找不到指定的使用者。</response>
        [HttpGet("{targetUserId:int}/profile")]
        [ProducesResponseType(typeof(ApiResponse<PublicUserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PublicUserProfileResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PublicUserProfileResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicUserProfile(int targetUserId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();
            var result = await _userService.GetPublicProfileAsync(targetUserId, userId, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取得目前登入使用者的個人資料。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。回傳目前登入使用者摘要、自我介紹、性別、身高、體重與生日
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的個人資料</returns>
        /// <response code="200">取得個人資料成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me/profile")]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetMyProfileAsync(userId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 更新目前登入使用者的個人資料。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。可更新暱稱、大頭貼、自我介紹、性別、身高、體重與生日。
        /// 未傳入的欄位不更新；自我介紹傳入空白時會清空。
        /// </remarks>
        /// <param name="request">更新個人資料請求</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>更新後的個人資料</returns>
        /// <response code="200">個人資訊更新成功。</response>
        /// <response code="400">請求內容不合法，例如暱稱空白、生日晚於今天、圖片已刪除或圖片用途不符。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="403">指定的大頭貼圖片不屬於目前登入使用者。</response>
        /// <response code="404">找不到目前登入使用者或指定的大頭貼圖片。</response>
        [Authorize]
        [HttpPut("me/profile")]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<MyUserProfileResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.UpdateMyProfileAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取得目前登入使用者的隱私個人資訊。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。回傳只有本人可查看的身高、體重與生日。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的隱私個人資訊</returns>
        /// <response code="200">取得個人隱私資料成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me/private-info")]
        [ProducesResponseType(typeof(ApiResponse<PrivateUserInfoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PrivateUserInfoResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PrivateUserInfoResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyPrivateInfo(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetMyPrivateInfoAsync(userId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 取得目前登入使用者發表的貼文列表。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。回傳目前登入使用者發表的分享文與提問文，排除已刪除貼文。
        /// </remarks>
        /// <param name="request">分頁查詢參數</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的貼文分頁列表</returns>
        /// <response code="200">取得發表分享/提問貼文成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me/posts")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyPosts([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetMyPostsAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取得目前登入使用者發表的委託文列表。
        /// </summary>
        /// <remarks>
        ///  需登入後使用。回傳目前登入使用者發表的委託文，包含已關閉的委託文。
        /// </remarks>
        /// <param name="request">分頁查詢參數</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的委託文分頁列表</returns>
        /// <response code="200">取得發表委託文成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me/commissions")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyCommissions([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetMyCommissionsAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取得目前登入使用者的收藏內容列表。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。可依 filter 查詢全部收藏、分享貼文、提問貼文或委託文。
        /// 列表依收藏時間由新到舊排序。
        /// </remarks>
        /// <param name="request">收藏內容查詢參數，包含分頁與內容類型篩選</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的收藏內容分頁列表</returns>
        /// <response code="200">取得收藏文章成功。</response>
        /// <response code="400">使用者 ID 或內容篩選條件不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me/saved-posts")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<HomepageItemResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMySavedPosts([FromQuery] UserContentQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetMySavedPostsAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }


        /// <summary>
        /// 取得目前登入使用者的追蹤中列表。
        /// </summary>
        /// <remarks>
        /// 需登入後使用。回傳目前登入使用者正在追蹤的使用者列表，排除已停用帳號。
        /// </remarks>
        /// <param name="request">分頁查詢參數</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的追蹤中分頁列表</returns>
        ///  <response code="200">取得追蹤中列表成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">尚未登入或 JWT 無效。</response>
        /// <response code="404">找不到目前登入使用者。</response>
        [Authorize]
        [HttpGet("me/following")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<FollowUserResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<FollowUserResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<FollowUserResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyFollowing([FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _userService.GetMyFollowingAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 取得指定使用者的粉絲列表。
        /// </summary>
        /// <remarks>
        /// 可未登入使用。若使用者已登入，會一併回傳目前登入使用者是否追蹤列表中的使用者。
        /// </remarks>
        /// <param name="targetUserId">目標使用者 ID</param>
        /// <param name="request">分頁查詢參數</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>指定使用者的粉絲分頁列表</returns>
        /// <response code="200">取得粉絲列表成功。</response>
        /// <response code="400">目標使用者 ID 或目前使用者 ID 不合法。</response>
        /// <response code="404">找不到指定的使用者。</response>
        [HttpGet("{targetUserId:int}/followers")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<FollowUserResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<FollowUserResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<FollowUserResponse>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFollowers( int targetUserId, [FromQuery] PagedQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();
            var result = await _userService.GetFollowersAsync(targetUserId, userId, request, cancellationToken);
            return ToActionResult(result);
        }


        // private helper
        private IActionResult ToActionResult<T>(ApiResponse<T> result)
        {
            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "INVALID_USER_ID" or
                "INVALID_TARGET_USER_ID" or
                "INVALID_CURRENT_USER_ID" or
                "REQUEST_REQUIRED" or
                "NICKNAME_REQUIRED" or
                "INVALID_BIRTH_DATE" or
                "INVALID_FILTER" or
                "AVATAR_IMAGE_DELETED" or
                "INVALID_AVATAR_IMAGE_PURPOSE" => BadRequest(result),

                "USER_NOT_FOUND" or
                "TARGET_USER_NOT_FOUND" or
                "AVATAR_IMAGE_NOT_FOUND" => NotFound(result),

                "AVATAR_IMAGE_NOT_OWNER" => StatusCode(StatusCodes.Status403Forbidden, result),

                _ => BadRequest(result)
            };
        }
    }
}
