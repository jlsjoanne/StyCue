using Microsoft.Extensions.Options;
using Stycue.Api.DTOs.Payment;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;
using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace Stycue.Api.Services
{
    public class EcpayPaymentGateway : IEcpayPaymentGateway
    {
        private readonly IOptions<EcpayOptions> _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EcpayPaymentGateway> _logger;

        public EcpayPaymentGateway(
            IOptions<EcpayOptions> options, HttpClient httpClient, ILogger<EcpayPaymentGateway> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
            ValidateOptions(_options.Value);
            _httpClient = httpClient;
            _logger = logger;
        }

        // 建單
        public EcpayCheckoutFormResponse CreateCheckoutForm(
            EcpayCheckoutRequest request)
        {
            ValidateCheckoutRequest(request);

            var options = _options.Value;
            var tpeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

            var merchantTradeDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tpeTimeZone)
                .ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            // 組合固定 ECPay 欄位與後端設定
            var formFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["MerchantID"] = options.MerchantId,
                ["MerchantTradeNo"] = request.MerchantTradeNo,
                ["MerchantTradeDate"] = merchantTradeDate,
                ["PaymentType"] = "aio",
                ["TotalAmount"] = request.TotalAmount.ToString(CultureInfo.InvariantCulture),
                ["TradeDesc"] = "StycuePointsPurchase",
                ["ItemName"] = request.ItemName,
                ["ChoosePayment"] = "Credit",
                ["ReturnURL"] = request.ReturnUrl,
                ["EncryptType"] = "1"
            };

            if(!string.IsNullOrWhiteSpace(request.ClientBackUrl))
            {
                formFields["ClientBackURL"] = request.ClientBackUrl;
            }

            // 將 CheckMacValue 放回 formFields
            formFields["CheckMacValue"] = GenerateCheckMacValue(formFields);

            return new EcpayCheckoutFormResponse
            {
                PaymentActionUrl = options.PaymentActionUrl,
                PaymentFormFields = formFields
            };
        }

        // 驗證簽章演算法
        public bool VerifyCheckMacValue(
            IReadOnlyDictionary<string, string> formFields)
        {
            if(formFields == null)
            {
                return false;
            }

            // 找出 ECPay 傳入的 CheckMacValue
            var receivedCheckMacValue = formFields
                .FirstOrDefault(pair => string.Equals(pair.Key, "CheckMacValue", StringComparison.OrdinalIgnoreCase))
                .Value;

            if (string.IsNullOrWhiteSpace(receivedCheckMacValue))
            {
                return false;
            }

            // 使用同一份完整欄位重算預期簽章
            var expectedCheckMacValue = GenerateCheckMacValue(formFields);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedCheckMacValue);

            // 將收到的值轉為大寫，對齊 ECPay 的大寫十六進位格式
            var receivedBytes = Encoding.UTF8.GetBytes(receivedCheckMacValue.Trim().ToUpperInvariant());

            // FixedTimeEquals進行固定時間比對 => 避免直接 == 的 timing attack 風險
            // timing attack（時間差攻擊）: 攻擊者可反覆猜測每一個字元，逐步推測正確的簽章值
            return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);

        }

        // 處理外部查單與回應驗簽
        public async Task<EcpayQueryTradeInfoResponse> QueryTradeInfoAsync(
            string merchantTradeNo, CancellationToken cancellationToken = default)
        {
            // 驗證 MerchantTradeNo + 產生 MerchantID、TimeStamp、CheckMacValue (private method)
            var formFields = CreateQueryTradeInfoFormFields(merchantTradeNo);

            // 把Dictionary => Http body + 自動設定 Content-Type: application/x-www-form-urlencoded
            using var content = new FormUrlEncodedContent(formFields);


            // 所有查單失敗都只能維持本地訂單 Pending，不能因為 timeout 或不完整回應而將訂單標記為 Paid。
            try
            {
                using var response = await _httpClient.PostAsync(
                    _options.Value.QueryTradeInfoUrl,
                    content, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if(!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ECPay Query Trade Info failed. MerchantTradeNo: {MerchantTradeNo}, StatusCode: {StatusCode}",
                        merchantTradeNo, (int)response.StatusCode);

                    throw new HttpRequestException(
                        $"ECPay Query Trade Info returned HTTP {(int)response.StatusCode}.");
                }

                // 解析 response、驗證 response 的 CheckMacValue、轉成內部 DTO。
                return ParseQueryTradeInfoResponse(responseContent);
            }
            // OperationCanceledException
            // - cancellationToken 不是使用者取消
            //   常見原因: _httpClient.Timeout 的 15 秒已到；ECPay 未及時回應
            //   處理意義: 視為外部查單 timeout，記錄後轉成 TimeoutException。訂單保持Pending，不可入帳。
            // - token 是使用者／上層取消
            //   常見原因: API request 被中止、服務關閉、上層主動取消
            //   處理意義: 不應誤判為 ECPay timeout；例外直接往上傳遞。
            catch (OperationCanceledException ex)
                when ( !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "ECPay Query Trade Info timed out. MerchantTradeNo: {MerchantTradeNo}", merchantTradeNo);

                throw new TimeoutException(
                    "ECPay Query Trade Info timed out.", ex);
            }
            // 常見原因: DNS 無法解析、TLS／連線失敗、ECPay 無法連線；或程式收到非 2xx HTTP
            // 處理意義: 代表外部 HTTP 呼叫失敗。記錄訂單編號與 status，不記錄密鑰或完整簽章。
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,
                    "ECPay Query Trade Info HTTP request failed. MerchantTradeNo: {MerchantTradeNo}",
                    merchantTradeNo);

                throw;
            }
            // 常見原因: ECPay response 為空、缺必要欄位、回應簽章錯誤、TradeStatus／TradeAmt 不能轉成整數
            // 處理意義: 代表回應不可被信任或格式異常；不可更新訂單或入帳。
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "ECPay Query Trade Info response validation failed. MerchantTradeNo: {MerchantTradeNo}",
                    merchantTradeNo);

                throw;
            }
        }


        // private methods

        // CreateCheckoutForm 與 VerifyCheckMacValue 共用，避免兩套簽章演算法不一致
        private string GenerateCheckMacValue(
            IReadOnlyDictionary<string, string> formFields)
        {
            ArgumentNullException.ThrowIfNull(formFields);

            // Where => 排除既有的 CheckMacValue 欄位，
            // CheckMacValue 是「其他欄位計算出來的結果」，不能把它本身再算進去
            // OrderBy => 排序是 ECPay 簽章規格的一部分；Dictionary 原本的加入順序不能作為計算依據
            var sortedParameters = formFields
                .Where(pair => !string.Equals(pair.Key, "CheckMacValue", StringComparison.OrdinalIgnoreCase))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value ?? string.Empty}");

            // 前後加上 HashKey／HashIV，
            var source = $"HashKey={_options.Value.HashKey}" +
                $"&{string.Join("&", sortedParameters)}" +
                $"&HashIV={_options.Value.HashIV}";

            // URL encode、轉小寫、
            var encodedSource = WebUtility.UrlEncode(source).ToLowerInvariant();

            // SHA-256
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(encodedSource));

            // 得到 CheckMacValue
            return Convert.ToHexString(hashBytes);
        }

        // 集中建立 Query Trade Info 的 MerchantID、MerchantTradeNo、時間戳與 CheckMacValue
        private Dictionary<string, string> CreateQueryTradeInfoFormFields(string merchantTradeNo)
        {
            if(string.IsNullOrWhiteSpace(merchantTradeNo))
            {
                throw new ArgumentException(
                    "MerchantTradeNo is required.", nameof(merchantTradeNo));
            }

            var formFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["MerchantID"] = _options.Value.MerchantId,
                ["MerchantTradeNo"] = merchantTradeNo,
                ["TimeStamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
            };

            formFields["CheckMacValue"] = GenerateCheckMacValue(formFields);

            return formFields;
        }

        // 解析並驗證 ECPay 查單結果
        // 集中處理 ECPay 查單回應的 form-urlencoded 欄位解析與 DTO 轉換
        private EcpayQueryTradeInfoResponse ParseQueryTradeInfoResponse(string responseContent)
        {
            // 擋掉空回應。查單沒有資料或外部服務異常時，不應把空內容當成有效交易
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                throw new InvalidOperationException("ECPay Query Trade Info returned an empty response.");
            }

            // ECPay 回應可視為 query string
            // QueryHelpers.ParseQuery 會拆成欄位集合並進行 URL decode
            // 通常預期字串前面有 ?，所以程式在沒有 ? 時補上
            var parseFields = QueryHelpers.ParseQuery(
                responseContent.StartsWith('?') ? responseContent : $"?{responseContent}");

            // 將上面的 query collection 轉成一般 Dictionary<string, string>
            var formFields = parseFields.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToString(),
                StringComparer.Ordinal);

            // 確認查單回應確實來自 ECPay，且內容沒有被竄改
            if (!VerifyCheckMacValue(formFields))
            {
                throw new InvalidOperationException("ECPay Query Trade Info CheckMacValue is invalid.");
            }

            // local function => 讀取必填欄位
            // 若 ECPay 回應少了關鍵欄位，會立刻失敗，而不會把空字串寫進本地訂單
            string GetRequiredValue(string key)
            {
                if( !formFields.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"ECPay Query Trade Info response is missing {key}.");
                }

                return value;
            }

            // 先安全轉成 int。ECPay 的 1 代表已付款
            // 是否入帳仍由 PointPurchaseService 判斷
            if (!int.TryParse(
                GetRequiredValue("TradeStatus"),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var tradeStatus))
            {
                throw new InvalidOperationException("ECPay Query Trade Info TradeStatus is invalid.");
            }

            // 將交易金額轉為整數
            // 之後 PointPurchaseService 必須拿它和 PointPurchaseOrder.AmountTwd 比對
            if ( !int.TryParse(
                GetRequiredValue("TradeAmt"),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var tradeAmt))
            {
                throw new InvalidOperationException("ECPay Query Trade Info TradeAmt is invalid.");
            }

            // 保留點數入帳判斷需要的欄位
            return new EcpayQueryTradeInfoResponse
            {
                TradeStatus = tradeStatus,
                MerchantTradeNo = GetRequiredValue("MerchantTradeNo"),
                TradeNo = GetRequiredValue("TradeNo"),
                TradeAmt = tradeAmt
            };
        }

        // Validate options
        private static void ValidateOptions(EcpayOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if ( string.IsNullOrWhiteSpace(options.MerchantId))
            {
                throw new InvalidOperationException("Ecpay:MerchantId is missing.");
            }

            if(string.IsNullOrWhiteSpace(options.HashKey))
            {
                throw new InvalidOperationException("Ecpay:HashKey is missing.");
            }

            if (string.IsNullOrWhiteSpace(options.HashIV))
            {
                throw new InvalidOperationException("Ecpay:HashIV is missing.");
            }

            if( !Uri.TryCreate(options.PaymentActionUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    "Ecpay:PaymentActionUrl must be an absolute URL.");
            }

            if( !Uri.TryCreate(options.QueryTradeInfoUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    "Ecpay:QueryTradeInfoUrl must be an absolute URL.");
            }

            if(string.IsNullOrWhiteSpace(options.ReturnUrl))
            {
                throw new InvalidOperationException("Ecpay:ReturnUrl is missing.");
            }
        }

        // 驗證request欄位
        private static void ValidateCheckoutRequest(EcpayCheckoutRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if(string.IsNullOrWhiteSpace(request.MerchantTradeNo))
            {
                throw new ArgumentException(
                    "MerchantTradeNo is required.", nameof(request));
            }

            // ECPay 建單要求特店訂單編號最多 20 字元且僅能使用英數字。
            // 資料庫的 unique index 負責唯一性；Gateway 則應防止不符合 ECPay 格式的值
            if (request.MerchantTradeNo.Length > 20 ||
                !request.MerchantTradeNo.All(char.IsAsciiLetterOrDigit))
            {
                throw new ArgumentException(
                    "MerchantTradeNo must contain 1 to 20 alphanumeric characters.", nameof(request));
            }

            if( request.TotalAmount <= 0)
            {
                throw new ArgumentException(
                    "TotalAmount must be greater than zero.", nameof(request));
            }

            if(string.IsNullOrWhiteSpace(request.ItemName))
            {
                throw new ArgumentException(
                    "ItemName is required.", nameof(request));
            }

            if(request.ItemName.Length > 400)
            {
                throw new ArgumentException(
                    "ItemName must not exceed 400 characters.", nameof(request));
            }

            if(string.IsNullOrWhiteSpace(request.ReturnUrl))
            {
                throw new ArgumentException(
                    "ReturnUrl is required.", nameof(request));
            }


            // Uri.TryCreate => 安全判斷字串能否解析成合法 URL
            // UriKind.Absolute => 要求必須是完整網址

            if ( request.ReturnUrl.Length > 200 || !Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException(
                    "ReturnUrl must be an absolute URL of 200 characters or fewer.", nameof(request));
            }

            if( !string.IsNullOrWhiteSpace(request.ClientBackUrl) && 
                (request.ClientBackUrl.Length > 200 || !Uri.TryCreate(request.ClientBackUrl, UriKind.Absolute, out _)))
            {
                throw new ArgumentException(
                    "ClientBackUrl must be an absolute URL of 200 characters or fewer.",
                    nameof(request));
            }
        }
    }
}
