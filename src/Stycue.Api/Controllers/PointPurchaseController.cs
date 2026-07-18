using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Payment;
using Stycue.Api.Extensions;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// 積分商城購買與綠界付款通知 API
    /// </summary>
    /// <remarks>
    /// 提供積分商品查詢、建立購買訂單、查詢目前使用者的購買訂單，
    /// 以及接收綠界 ECPay 的 server-to-server 付款結果通知。
    /// </remarks>
    [Route("api")]
    [ApiController]
    [Tags("Payments")]
    public class PointPurchaseController : ControllerBase
    {
        private readonly IPointPurchaseService _pointPurchaseService;
        private readonly ILogger<PointPurchaseController> _logger;

        public PointPurchaseController(IPointPurchaseService pointPurchaseService, ILogger<PointPurchaseController> logger)
        {
            _pointPurchaseService = pointPurchaseService;
            _logger = logger;
        }

        /// <summary>
        /// 取得可購買的積分方案
        /// </summary>
        /// <remarks>
        /// 僅回傳啟用中的積分商品，依 DisplayOrder 由小到大排序
        /// </remarks>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>可購買的積分方案清單</returns>
        /// <response code="200">成功取得積分方案，可能為空清單。</response>
        /// <response code="401">未登入或 JWT 無效。</response>
        /// <response code="500">伺服器發生未預期錯誤。</response>
        [Authorize]
        [HttpGet("points/products")]
        [ProducesResponseType(typeof(ApiResponse<List<PointProductResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
        {
            var result = await _pointPurchaseService.GetProductsAsync(cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 建立積分購買訂單
        /// </summary>
        /// <remarks>
        /// 前端僅傳入 PointProductId。後端會依商品資料建立 Pending 訂單、
        /// 寫入金額與點數 snapshot，並回傳 ECPay 隱藏付款表單所需資料。
        /// </remarks>
        /// <param name="request">欲購買的積分商品</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>新建訂單與 ECPay 付款表單資料</returns>
        /// <response code="200">訂單建立成功。</response>
        /// <response code="400">request 或積分商品 ID 不合法。</response>
        /// <response code="401">未登入或 JWT 無效。</response>
        /// <response code="404">積分商品不存在或目前未開放購買。</response>
        /// <response code="500">訂單建立或付款表單產生時發生未預期錯誤。</response>
        [Authorize]
        [HttpPost("points/purchases")]
        [ProducesResponseType(typeof(ApiResponse<CreatePointPurchaseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreatePointPurchaseResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<CreatePointPurchaseResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostPurchase(
            [FromBody] CreatePointPurchaseRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _pointPurchaseService.CreateAsync(userId, request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 取得目前登入使用者的指定積分購買訂單
        /// </summary>
        /// <remarks>
        /// 僅能查詢目前登入使用者自己的訂單；訂單不存在或不屬於目前使用者時，
        /// 一律回傳找不到訂單，避免洩漏其他使用者的訂單資訊。
        /// </remarks>
        /// <param name="orderId">積分購買訂單 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>積分購買訂單詳細資料</returns>
        /// <response code="200">成功取得訂單。</response>
        /// <response code="400">訂單 ID 不合法。</response>
        /// <response code="401">未登入或 JWT 無效。</response>
        /// <response code="404">找不到訂單，或訂單不屬於目前使用者。</response>
        /// <response code="500">伺服器發生未預期錯誤。</response>
        [Authorize]
        [HttpGet("points/purchases/{orderId:int}")]
        [ProducesResponseType(typeof(ApiResponse<PointPurchaseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PointPurchaseResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<PointPurchaseResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPurchase(int orderId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var result = await _pointPurchaseService.GetAsync(userId, orderId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// 接收綠界 ECPay 付款結果通知
        /// </summary>
        /// <remarks>
        /// 此 endpoint 供綠界以 application/x-www-form-urlencoded server-to-server POST 呼叫，
        /// 不需 JWT。後端會驗證 CheckMacValue、MerchantID、模擬付款狀態，
        /// 再向綠界查單並與本地訂單交叉比對後執行點數入帳。
        ///
        /// 成功處理時必須回傳純文字 1|OK；若未回傳 1|OK，綠界可能重送通知。
        /// SimulatePaid=1 僅確認 ReturnURL 可接收通知，不會變更訂單狀態或入帳。
        /// </remarks>
        /// <param name="request">綠界以 form data 傳送的付款結果通知</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>成功時回傳純文字 1|OK</returns>
        /// <response code="200">已成功接收並處理或確認付款通知。</response>
        /// <response code="400">綠界 form data 無法完成 model binding。</response>
        /// <response code="500">通知驗證、查單或入帳未完成；綠界可依機制重送通知。</response>
        [AllowAnonymous]
        [HttpPost("payments/ecpay/return")]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessEcpayReturn(
            [FromForm] EcpayPaymentReturnRequest request, CancellationToken cancellationToken)
        {
            var form = await Request.ReadFormAsync(cancellationToken);

            var formFields = form.ToDictionary(
                field => field.Key,
                field => field.Value.ToString(),
                StringComparer.Ordinal);

            _logger.LogInformation(
                "Received ECPay callback. TraceId: {TraceId}, MerchantTradeNo: {MerchantTradeNo}, RtnCode: {RtnCode}, SimulatePaid: {SimulatePaid}",
                HttpContext.TraceIdentifier, request.MerchantTradeNo, request.RtnCode, request.SimulatePaid);

            var result = await _pointPurchaseService.ProcessEcpayReturnAsync(request, formFields, cancellationToken);

            if(!result.Success)
            {
                _logger.LogWarning(
                    "ECPay callback was not applied. TraceId: {TraceId}, MerchantTradeNo: {MerchantTradeNo}, ErrorCode: {ErrorCode}",
                    HttpContext.TraceIdentifier, request.MerchantTradeNo, result.ErrorCode);

                // 已確認收到且判定為付款失敗；不入帳，但仍應 acknowledge。
                if (result.ErrorCode == "ECPAY_PAYMENT_NOT_SUCCESS")
                {
                    return Content("1|OK", "text/plain");
                }

                // 其餘失敗，例如查單未確認、簽章異常、交易資料不一致，
                // 不回 1|OK，讓 ECPay 重送並留下 log。
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Content("1|OK", "text/plain");
        }

        // private method
        private IActionResult ToActionResult<T>(ApiResponse<T> result)
        {
            if(result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                // Request / route / user input validation
                "INVALID_USER_ID" or
                "INVALID_ORDER_ID" or
                "INVALID_POINT_PURCHASE_REQUEST" or
                "INVALID_POINT_PRODUCT_ID" => BadRequest(result),

                // 商品不存在、已停用，或訂單不存在／不屬於目前使用者
                "POINT_PRODUCT_NOT_AVAILABLE" or
                "POINT_PURCHASE_ORDER_NOT_FOUND" => NotFound(result),

                // 訂單目前狀態與欲執行操作衝突
                "POINT_PURCHASE_STATUS_NOT_PAYABLE" or
                "POINT_PURCHASE_CONCURRENCY_CONFLICT" => Conflict(result),

                // 三次隨機編號皆撞 unique index，不是前端 request 錯誤
                "MERCHANT_TRADE_NO_GENERATION_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, result),

                _ => BadRequest(result)
            };
        }
    }
}
