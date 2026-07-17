namespace Stycue.Api.DTOs.Payment
{
    // 給 IEcpayPaymentGateway 使用
    public class EcpayCheckoutRequest
    {
        public string MerchantTradeNo { get; set; } = string.Empty;
        public int TotalAmount { get; set; }
        public string ItemName { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;
        public string ClientBackUrl { get; set; } = string.Empty;
    }
}
