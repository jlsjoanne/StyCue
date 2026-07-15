using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Commissions;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 委託文 API
    /// </summary>
    /// <remarks>
    /// 提供委託文詳情查詢、建立、提前關閉、到期後補充並重新開啟、加碼積分，以及手動選擇最佳留言發獎功能
    /// 委託文不提供一般編輯與刪除；生命週期由 Close、Repost、Boost、手動選擇最佳留言與背景補結算流程處理
    /// </remarks>
    [Route("api/commissions")]
    [ApiController]
    [Tags("Commissions")]
    public class CommissionController : ControllerBase
    {
        private readonly ICommissionService _commissionService;

        public CommissionController(ICommissionService commissionService)
        {
            _commissionService = commissionService;
        }

        /// <summary>
        /// 取得委託文詳情
        /// </summary>
        /// <remarks>
        /// 可未登入查詢
        /// 若使用者已登入，回應會包含目前使用者是否為委託建立者，以及是否可執行加碼、重新開啟、關閉等操作
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>委託文詳情</returns>
        /// <response code="200">成功取得委託文詳情。</response>
        /// <response code="400">請求參數錯誤。</response>
        /// <response code="404">找不到指定的委託文。</response>
        [HttpGet("{commissionId:int}")]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCommission(int commissionId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserIdOrNull();

            var result = await _commissionService.GetCommissionAsync(userId, commissionId, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 建立委託文
        /// </summary>
        /// <remarks>
        /// 需登入後使用。
        /// 建立委託時會驗證圖片與標籤，並預扣委託者設定的懸賞積分。
        /// 圖片需先透過圖片上傳 API 取得 imageId，再由此 API 綁定到委託文。
        /// 建立成功後，委託文狀態為進行中，並回傳最新委託文詳情。
        /// </remarks>
        /// <param name="request">建立委託文請求，包含標題、內容、身高、體重、年齡、預算、懸賞積分、圖片 ID 與標籤 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>建立完成後的委託文詳情</returns>
        /// <response code="201">委託文建立成功。</response>
        /// <response code="400">請求內容驗證失敗，或圖片、標籤、積分資料不符合規則。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="409">目前狀態或資源條件不允許建立委託文。</response>
        /// <response code="500">委託文建立後無法取得詳情。</response>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CreateCommissionRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commissionService.CreateAsync(
                userId, request, cancellationToken);

            if(!result.Success)
            {
                return ToActionResult(result);
            }

            return CreatedAtAction(nameof(GetCommission),
                new { commissionId = result.Data!.CommissionId },
                result);

        }


        /// <summary>
        /// 提前關閉委託文
        /// </summary>
        /// <remarks>
        /// 需登入後使用，且只有委託建立者可以執行
        /// 此 API 僅能在建立委託後的提前關閉期限內使用，目前規則為建立後 12 小時內
        /// 關閉成功後會將委託狀態改為已關閉，標記結算時間，並立即依系統設定退還部分積分給委託者
        /// 提前關閉的退點不交由背景結算服務處理
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>提前關閉結果與退還積分資訊</returns>
        /// <response code="200">委託文已成功提前關閉。</response>
        /// <response code="400">請求參數錯誤。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">目前使用者不是委託建立者。</response>
        /// <response code="404">找不到指定的委託文。</response>
        /// <response code="409">委託文狀態不允許關閉、已超過提前關閉期限，或已完成退點/結算。</response>
        [Authorize]
        [HttpPost("{commissionId:int}/close")]
        [ProducesResponseType(typeof(ApiResponse<CloseCommissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CloseCommissionResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CloseCommissionResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<CloseCommissionResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CloseCommissionResponse>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Close(int commissionId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commissionService.CloseAsync(userId, commissionId, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// 到期後補充內容並重新開啟委託文
        /// </summary>
        /// <remarks>
        /// 需登入後使用，且只有委託建立者可以執行。
        /// 此 API 用於委託文到期後補充說明並重新開啟委託，不會覆蓋原始委託內容。
        /// 一篇委託文只能重新開啟一次。
        /// 補充圖片會綁定到本次補充內容；補充標籤會合併到委託文整體標籤。
        /// additionalPoints 可為 0；若大於 0，會預扣委託者追加的積分。
        /// 重新開啟成功後，委託狀態會回到進行中，並依系統設定重新延長到期時間。
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="request">重新開啟委託文請求，包含補充內容、追加積分、圖片 ID 與標籤 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>重新開啟後的最新委託文詳情</returns>
        /// <response code="200">委託文已成功重新開啟。</response>
        /// <response code="400">請求內容驗證失敗，或圖片、標籤、積分資料不符合規則。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">目前使用者不是委託建立者。</response>
        /// <response code="404">找不到指定的委託文。</response>
        /// <response code="409">委託文尚未到期、狀態不允許重新開啟，或已達重新開啟次數限制。</response>
        /// <response code="500">重新開啟後無法取得委託文詳情。</response>
        [Authorize]
        [HttpPost("{commissionId:int}/repost")]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<CommissionDetailResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Repost(int commissionId, 
            [FromBody] RepostCommissionRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _commissionService.RepostAsync(userId, commissionId, request, cancellationToken);

            return ToActionResult(result);
        }

        /// <summary>
        /// 加碼委託文積分並延長到期時間
        /// </summary>
        /// <remarks>
        /// 需登入後使用，且只有委託建立者可以執行。
        /// 加碼時會預扣委託者追加的積分，增加委託文目前懸賞積分，並依系統設定重新延長到期時間。
        /// 只要委託尚未關閉、尚未發獎、尚未流標且尚未結算，即可加碼。
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="request">加碼請求，包含追加積分(追加積分需大於 0)</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>加碼後的委託文積分、狀態與到期時間資訊</returns>
        /// <response code="200">委託文加碼成功。</response>
        /// <response code="400">請求內容驗證失敗，或追加積分不符合規則。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">目前使用者不是委託建立者。</response>
        /// <response code="404">找不到指定的委託文。</response>
        /// <response code="409">委託文狀態不允許加碼，或委託已發獎/結算。</response>
        [Authorize]
        [HttpPost("{commissionId:int}/boost")]
        [ProducesResponseType(typeof(ApiResponse<BoostCommissionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BoostCommissionResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<BoostCommissionResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<BoostCommissionResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<BoostCommissionResponse>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Boost(int commissionId,
            [FromBody] BoostCommissionRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _commissionService.BoostAsync(userId, commissionId, request, cancellationToken);

            return ToActionResult(result);
        }


        /// <summary>
        /// 選擇最佳留言並發放委託積分
        /// </summary>
        /// <remarks>
        /// 需登入後使用，且只有委託建立者可以執行
        /// 此 API 用於委託者手動選擇一則委託根留言作為最佳留言，並立即發放扣除平台手續比例後的委託積分給該留言作者
        /// 只能選擇未刪除的根留言，不能選擇回覆留言
        /// 成功後委託文狀態會更新為已發獎，並設定 AwardedCommentId、AwardedAt 與 RewardSettledAt，避免背景結算服務重複處理
        /// 已關閉、已流標、已發獎或已結算的委託不可再次選擇最佳留言
        /// </remarks>
        /// <param name="commissionId">委託文 ID</param>
        /// <param name="request">選擇最佳留言請求，包含要選為最佳留言的 commentId</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>最佳留言發獎結果，包含委託文狀態、得獎留言、得獎使用者、發放積分與得獎者錢包資訊</returns>
        /// <response code="200">最佳留言已選擇，積分已發放。</response>
        /// <response code="400">請求內容驗證失敗，或指定留言不可被選為最佳留言。</response>
        /// <response code="401">尚未登入。</response>
        /// <response code="403">目前使用者不是委託建立者。</response>
        /// <response code="404">找不到指定的委託文。</response>
        /// <response code="409">委託文狀態不允許選擇最佳留言，或委託已發獎/結算。</response>
        /// <response code="500">選擇最佳留言流程發生未預期錯誤。</response>
        [Authorize]
        [HttpPost("{commissionId:int}/best-comment")]
        [ProducesResponseType(typeof(ApiResponse<CommissionRewardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CommissionRewardResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CommissionRewardResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<CommissionRewardResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CommissionRewardResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<CommissionRewardResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SelectBestComment(
            int commissionId, [FromBody] SelectBestCommentRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _commissionService.SelectBestCommentAsync(
                userId, commissionId, request, cancellationToken);

            return ToActionResult(result);
        }


        // private helper for return http status code
        private IActionResult ToActionResult<T>(ApiResponse<T> result)
        {
            if(result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                // 伺服器流程錯誤
                "COMMISSION_DETAIL_NOT_FOUND_AFTER_CREATE" or
                "COMMISSION_DETAIL_NOT_FOUND_AFTER_REPOST" or
                "COMMISSION_CREATE_FAILED" or
                "COMMISSION_CLOSE_FAILED" or
                "COMMISSION_REPOST_FAILED" or
                "COMMISSION_BOOST_FAILED" or
                "COMMISSION_SELECT_BEST_COMMENT_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, result),

                // 找不到資源
                "COMMISSION_NOT_FOUND" or
                "USER_NOT_FOUND" => NotFound(result),

                // 權限不足
                "COMMISSION_NOT_OWNER" or
                "FORBIDDEN" or
                "IMAGE_NOT_OWNER" => StatusCode(StatusCodes.Status403Forbidden, result),

                // 狀態衝突或商業規則衝突
                "COMMISSION_CANNOT_CLOSE" or
                "COMMISSION_CLOSE_WINDOW_EXPIRED" or
                "COMMISSION_ALREADY_REFUNDED" or
                "COMMISSION_CANNOT_REPOST" or
                "COMMISSION_REPOST_LIMIT_REACHED" or
                "COMMISSION_CANNOT_BOOST" or
                "INSUFFICIENT_POINTS" or
                "IMAGE_ALREADY_BOUND" or
                "COMMISSION_REWARD_ALREADY_SETTLED" or
                "COMMISSION_CLOSED"  or
                "COMMISSION_NO_AWARD"  or
                "COMMISSION_CANNOT_SELECT_BEST_COMMENT" or
                "COMMISSION_REWARD_ALREADY_PAID" or
                "INVALID_REWARD_POINTS" => Conflict(result),

                // 其餘多半是 request validation 或設定值錯誤
                _ => BadRequest(result)
            };
        }
    }
}
