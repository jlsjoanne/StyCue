using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Points;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 積分相關API
    /// </summary>
    [Authorize]
    [Route("api/points")]
    [ApiController]
    [Tags("Points")]
    public class PointController : ControllerBase
    {
        private readonly IPointService _pointService;

        public PointController(IPointService pointService)
        {
            _pointService = pointService;
        }

        /// <summary>
        /// 查詢目前登入使用者的積分錢包
        /// </summary>
        /// <remarks>
        /// 回傳目前可用積分、累計取得積分與累計花費積分。
        /// 若使用者尚未建立積分錢包，後端會建立 0 點錢包後回傳。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>目前登入使用者的積分錢包資訊</returns>
        /// <response code="200">查詢成功。</response>
        /// <response code="400">使用者 ID 不合法。</response>
        /// <response code="401">未登入或登入資訊無效。</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<PointWalletResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PointWalletResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserPoint(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _pointService.GetMyWalletAsync(userId, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// 領取每日積分
        /// </summary>
        /// <remarks>
        ///  每位使用者每日只能領取一次。日期基準使用台灣時區。
        ///  若今日已領取，仍回傳 200，並回傳今日領取紀錄與目前可用積分。
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>每日積分領取結果</returns>
        /// <response code="200">領取成功，或今日已領取。</response>
        /// <response code="400">使用者 ID 不合法，或每日積分設定錯誤。</response>
        /// <response code="401">未登入或登入資訊無效。</response>
        [HttpPost("daily")]
        [ProducesResponseType(typeof(ApiResponse<DailyPointClaimResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<DailyPointClaimResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ClaimDailyPoint(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _pointService.ClaimDailyAsync(userId, cancellationToken);

            if(!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// 查詢目前登入使用者的積分交易紀錄
        /// </summary>
        /// <remarks>
        /// 支援依交易類型、關聯來源類型、關聯來源 ID 與建立時間區間篩選，並回傳分頁結果
        /// </remarks>
        /// <param name="request">積分交易紀錄查詢條件與分頁參數</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>積分交易紀錄分頁結果</returns>
        /// <response code="200">查詢成功。</response>
        /// <response code="400">查詢條件不合法。</response>
        /// <response code="401">未登入或登入資訊無效。</response>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<PointTransactionResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<PointTransactionResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTransactions([FromQuery] PointTransactionQueryRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _pointService.GetTransactionsAsync(userId, request, cancellationToken);

            if(!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

    }
}
