using Stycue.Api.Enums;
using System.Text.Json.Serialization;
using Stycue.Api.Converters;

namespace Stycue.Api.DTOs.Payment
{
    public class CreatePointPurchaseResponse
    {
        public int OrderId { get; set; }
        public string MerchantTradeNo { get; set; } = string.Empty;
        public PointPurchaseStatus Status { get; set; }

        public EcpayCheckoutFormResponse Checkout { get; set; } = null!;
    }

    public class EcpayCheckoutFormResponse
    {
        public string PaymentActionUrl { get; set; } = string.Empty;

        // 前端用這些欄位建立隱藏 form 後 POST submit 至綠界。
        [JsonConverter(typeof(PreserveStringDictionaryKeyJsonConverter))]
        public Dictionary<string, string> PaymentFormFields { get; set; } = [];
    }
}
