using AutoMapper;
using Microsoft.Extensions.Options;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Payment;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Stycue.Api.Enums;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Stycue.Api.Services
{
    public class PointPurchaseService : IPointPurchaseService
    {
        private readonly AppDbContext _dbContext;
        private readonly IEcpayPaymentGateway _ecpayPaymentGateway;
        private readonly IPointService _pointService;
        private readonly IOptions<EcpayOptions> _options;
        private readonly IMapper _mapper;
        private readonly ILogger<PointPurchaseService> _logger;

        public PointPurchaseService(
            AppDbContext dbContext,
            IEcpayPaymentGateway ecpayPaymentGateway,
            IPointService pointService, IOptions<EcpayOptions> options,
            IMapper mapper, ILogger<PointPurchaseService> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            _dbContext = dbContext;
            _ecpayPaymentGateway = ecpayPaymentGateway;
            _pointService = pointService;
            _options = options;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<PointProductResponse>>> GetProductsAsync(
            CancellationToken cancellationToken = default)
        {
            var products = await _dbContext.PointProducts.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
                .ToListAsync(cancellationToken);

            var response = _mapper.Map<List<PointProductResponse>>(products);

            return ApiResponse<List<PointProductResponse>>.SuccessResult(response, "取得積分方案成功");
        }

        public async Task<ApiResponse<CreatePointPurchaseResponse>> CreateAsync(
            int userId, CreatePointPurchaseRequest request, CancellationToken cancellationToken = default)
        {
            if (ValidateUserId<CreatePointPurchaseResponse>(userId) is { } userError)
            {
                return userError;
            }

            if (request == null)
            {
                return ApiResponse<CreatePointPurchaseResponse>.FailResult(
                    "請提供購買方案", "INVALID_POINT_PURCHASE_REQUEST");
            }

            if (request.PointProductId <= 0)
            {
                return ApiResponse<CreatePointPurchaseResponse>.FailResult(
                    "不合法的積分方案 ID", "INVALID_POINT_PRODUCT_ID");
            }

            var product = await FindActiveProductAsync(request.PointProductId, cancellationToken);

            if (product == null)
            {
                return ApiResponse<CreatePointPurchaseResponse>.FailResult(
                    "積分方案不存在或目前未開放購買", "POINT_PRODUCT_NOT_AVAILABLE");
            }

            return await CreatePendingOrderWithCheckoutAsync(userId, product, cancellationToken);

        }

        public async Task<ApiResponse<PointPurchaseResponse>> GetAsync(
            int userId, int orderId, CancellationToken cancellationToken = default)
        {
            if (ValidateUserId<PointPurchaseResponse>(userId) is { } userError)
            {
                return userError;
            }

            if (ValidateOrderId<PointPurchaseResponse>(orderId) is { } orderError)
            {
                return orderError;
            }

            var order = await FindOwnedOrderAsync(userId, orderId, cancellationToken);

            if (order == null)
            {
                return ApiResponse<PointPurchaseResponse>.FailResult(
                    "找不到此購買訂單", "POINT_PURCHASE_ORDER_NOT_FOUND");
            }

            var response = _mapper.Map<PointPurchaseResponse>(order);

            return ApiResponse<PointPurchaseResponse>.SuccessResult(response, "取得購買訂單成功");
        }

        public async Task<ApiResponse<object>> ProcessEcpayReturnAsync(
            EcpayPaymentReturnRequest request,
            IReadOnlyDictionary<string, string> formFields, CancellationToken cancellationToken = default)
        {
            if (request == null || formFields == null || formFields.Count == 0)
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知資料不完整", "ECPAY_CALLBACK_INVALID");
            }

            if ( string.IsNullOrWhiteSpace(request.MerchantTradeNo))
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知缺少商店交易編號", "ECPAY_MERCHANT_TRADE_NO_REQUIRED");
            }

            if (string.IsNullOrWhiteSpace(request.TradeNo))
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知缺少綠界交易編號", "ECPAY_TRADE_NO_REQUIRED");
            }

            if (request.TradeAmt <= 0)
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知的交易金額不正確", "ECPAY_TRADE_AMOUNT_INVALID");
            }

            if( string.IsNullOrWhiteSpace(request.PaymentDate))
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知缺少付款時間", "ECPAY_PAYMENT_DATE_REQUIRED");
            }

            if( !_ecpayPaymentGateway.VerifyCheckMacValue(formFields))
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知簽章驗證失敗", "ECPAY_CHECK_MAC_INVALID");
            }

            if( request.MerchantID != _options.Value.MerchantId)
            {
                return ApiResponse<object>.FailResult(
                    "綠界商店代號不一致", "ECPAY_MERCHANT_ID_MISMATCH");
            }

            if( request.SimulatePaid == 1)
            {
                return ApiResponse<object>.SuccessResult(
                    new { }, "已收到綠界模擬付款通知，未執行點數入帳");
            }

            if(request.RtnCode != 1)
            {
                return ApiResponse<object>.FailResult(
                    "綠界回傳付款未成功", "ECPAY_PAYMENT_NOT_SUCCESS");
            }

            var order = await FindOrderForPaymentAsync(request.MerchantTradeNo, cancellationToken);

            if( order == null)
            {
                return ApiResponse<object>.FailResult(
                    "找不到對應的購買訂單", "POINT_PURCHASE_ORDER_NOT_FOUND");
            }

            if( !TryParseEcpayPaymentDateUtc(request.PaymentDate, out var paidAtUtc))
            {
                return ApiResponse<object>.FailResult(
                    "綠界回傳的付款時間格式不正確", "INVALID_ECPAY_PAYMENT_DATE");
            }

            var queryResult = await _ecpayPaymentGateway.QueryTradeInfoAsync(request.MerchantTradeNo, cancellationToken);

            if( request.MerchantTradeNo != queryResult.MerchantTradeNo)
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知與查單結果的商店交易編號不一致", "ECPAY_MERCHANT_TRADE_NO_MISMATCH");
            }

            if( request.TradeNo != queryResult.TradeNo)
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知與查單結果的交易編號不一致", "ECPAY_TRADE_NO_MISMATCH");
            }

            if( request.TradeAmt != queryResult.TradeAmt)
            {
                return ApiResponse<object>.FailResult(
                    "綠界付款通知與查單結果的交易金額不一致", "ECPAY_TRADE_AMOUNT_MISMATCH");
            }

            return await ApplyPaidOrderAsync(order, queryResult, paidAtUtc, cancellationToken);
        }


        // private methods

        private static ApiResponse<T>? ValidateUserId<T>(int userId)
        {
            return userId > 0
                  ? null
                  : ApiResponse<T>.FailResult(
                      "不合法的使用者 ID",
                      "INVALID_USER_ID");
        }

        private static ApiResponse<T>? ValidateOrderId<T>(int orderId)
        {
            return orderId > 0
                  ? null
                  : ApiResponse<T>.FailResult(
                      "不合法的訂單 ID",
                      "INVALID_ORDER_ID");
        }

        private async Task<PointProduct?> FindActiveProductAsync(
            int productId, CancellationToken cancellationToken)
        {
            return await _dbContext.PointProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);
        }

        private static string GenerateMerchantTradeNo()
        {
            Span<byte> randomBytes = stackalloc byte[9];

            RandomNumberGenerator.Fill(randomBytes);

            return $"ST{Convert.ToHexString(randomBytes)}";
        }

        private static bool TryParseEcpayPaymentDateUtc(
            string? paymentDate, out DateTime paidAtUtc)
        {
            paidAtUtc = default;

            if (string.IsNullOrWhiteSpace(paymentDate))
            {
                return false;
            }

            const string format = "yyyy/MM/dd HH:mm:ss";

            if( !DateTime.TryParseExact(
                paymentDate.Trim(),
                format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var taipeiLocalTime))
            {
                return false;
            }

            var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Taipei Standard Time" : "Asia/Taipei");

            paidAtUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(taipeiLocalTime, DateTimeKind.Unspecified),
                taipeiTimeZone);

            return true;
        }

        private EcpayCheckoutRequest BuildCheckoutRequest(PointPurchaseOrder order, PointProduct product)
        {
            return new EcpayCheckoutRequest
            {
                MerchantTradeNo = order.MerchantTradeNo,
                TotalAmount = order.AmountTwd,
                ItemName = product.Name,
                ReturnUrl = _options.Value.ReturnUrl,
                ClientBackUrl = string.IsNullOrWhiteSpace(_options.Value.ClientBackUrl) ? null : _options.Value.ClientBackUrl
            };
        }

        private async Task<PointPurchaseOrder?> FindOwnedOrderAsync(int userId, int orderId, CancellationToken cancellationToken)
        {
            return await _dbContext.PointPurchaseOrders
                .AsNoTracking()
                .Include(o => o.PointProduct)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);
        }

        private async Task<PointPurchaseOrder?> FindOrderForPaymentAsync(string merchantTradeNo, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(merchantTradeNo))
            {
                return null;
            }

            return await _dbContext.PointPurchaseOrders
                .Include(o => o.PointProduct)
                .FirstOrDefaultAsync(o => o.MerchantTradeNo == merchantTradeNo, cancellationToken);
        }

        // 將訂單加入DB
        // 因為MerchantTradeNo是unique index，因為可能有重複編號產生衝突，最多嘗試三次產出隨機編號

        // 偵測DB unique index／unique constraint 衝突錯誤
        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.GetBaseException() is SqlException
            {
                Number: 2601 or 2627
            };
        }

        // 建立 Pending 訂單、處理 MerchantTradeNo 撞號重試、產生 checkout form

        private async Task<ApiResponse<CreatePointPurchaseResponse>> CreatePendingOrderWithCheckoutAsync(
            int userId, PointProduct product, CancellationToken cancellationToken)
        {
            const int maxAttempt = 3;

            for (var attempt = 1; attempt <= maxAttempt; attempt++)
            {
                var now = DateTime.UtcNow;

                var order = new PointPurchaseOrder
                {
                    MerchantTradeNo = GenerateMerchantTradeNo(),
                    UserId = userId,
                    PointProductId = product.Id,
                    AmountTwd = product.PriceTwd,
                    Points = product.Points,
                    PaymentProvider = PaymentProvider.Ecpay,
                    PaymentMethod = PaymentMethod.CreditCard,
                    Status = PointPurchaseStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var checkout = _ecpayPaymentGateway.CreateCheckoutForm(
                    BuildCheckoutRequest(order, product));

                _dbContext.PointPurchaseOrders.Add(order);

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return ApiResponse<CreatePointPurchaseResponse>.SuccessResult(
                        new CreatePointPurchaseResponse
                        {
                            OrderId = order.Id,
                            MerchantTradeNo = order.MerchantTradeNo,
                            Status = order.Status,
                            Checkout = checkout
                        }, "訂單建立成功，請前往付款");
                }
                catch(DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    _dbContext.Entry(order).State = EntityState.Detached;

                    _logger.LogWarning(ex,
                        "MerchantTradeNo collision. Attempt: {Attempt}/{MaxAttempts}",
                        attempt, maxAttempt);
                }
            }

            return ApiResponse<CreatePointPurchaseResponse>.FailResult(
                "訂單建立失敗，請稍後再試", "MERCHANT_TRADE_NO_GENERATION_FAILED");
        }

        private async Task<ApiResponse<object>> ApplyPaidOrderAsync(
            PointPurchaseOrder order, EcpayQueryTradeInfoResponse queryResult,
            DateTime paidAtUtc, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(order);
            ArgumentNullException.ThrowIfNull(queryResult);

            if( !string.Equals(order.MerchantTradeNo, queryResult.MerchantTradeNo, StringComparison.Ordinal))
            {
                return ApiResponse<object>.FailResult(
                    "綠界訂單編號與資料庫訂單不一致", "ECPAY_ORDER_MISMATCH");
            }

            if( queryResult.TradeAmt != order.AmountTwd)
            {
                return ApiResponse<object>.FailResult(
                    "綠界交易金額與資料庫訂單不一致", "ECPAY_AMOUNT_MISMATCH");
            }

            if( queryResult.TradeStatus != 1)
            {
                return ApiResponse<object>.FailResult(
                    "綠界查單結果尚未確認付款成功", "ECPAY_PAYMENT_NOT_CONFIRMED");
            }

            // 重複 callback：已完成訂單直接成功，不可再次入帳。
            if(order.Status == PointPurchaseStatus.Paid)
            {
                return ApiResponse<object>.SuccessResult(
                    new { OrderId = order.Id }, "訂單已完成付款");
            }

            if( order.Status != PointPurchaseStatus.Pending)
            {
                return ApiResponse<object>.FailResult(
                    "目前訂單狀態不可執行付款入帳", "POINT_PURCHASE_STATUS_NOT_PAYABLE");
            }

            var orderId = order.Id;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                order.Status = PointPurchaseStatus.Paid;
                order.ProviderTradeNo = queryResult.TradeNo;
                order.PaidAt = paidAtUtc;
                order.UpdatedAt = DateTime.UtcNow;

                // 先儲存訂單狀態，讓 RowVersion 成為第一道 concurrency guard。
                await _dbContext.SaveChangesAsync(cancellationToken);

                var description = string.IsNullOrWhiteSpace(order.PointProduct?.Name)
                    ? "購買點數" : $"購買點數 : {order.PointProduct.Name}";

                var pointResult = await _pointService.AddPointsAsync(
                    order.UserId, order.Points,
                    PointTransactionType.PointPurchase, PointReferenceType.PointPurchaseOrder,
                    order.Id, description, cancellationToken);

                if(!pointResult.Success)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ApiResponse<object>.FailResult(pointResult.Message, pointResult.ErrorCode);
                }

                // AddPointsAsync 的 SaveChangesAsync 會在同一個 scoped DbContext 
                // 同一個 transaction 中寫入錢包與 PointTransaction
                await transaction.CommitAsync(cancellationToken);

                return ApiResponse<object>.SuccessResult(new { OrderId = orderId }, "付款成功，點數已入帳");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogWarning(
                    ex, "Point purchase payment concurrency conflict. OrderId: {OrderId}", orderId);

                _dbContext.ChangeTracker.Clear();

                var latestOrder = await _dbContext.PointPurchaseOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if(latestOrder?.Status == PointPurchaseStatus.Paid)
                {
                    return ApiResponse<object>.SuccessResult(
                        new { OrderId = orderId }, "訂單已完成付款");
                }

                return ApiResponse<object>.FailResult(
                    "付款入帳發生併發衝突，請稍後查詢訂單狀態", "POINT_PURCHASE_CONCURRENCY_CONFLICT");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
