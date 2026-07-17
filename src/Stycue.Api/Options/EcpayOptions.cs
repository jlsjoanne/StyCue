namespace Stycue.Api.Options
{
    public class EcpayOptions
    {
        public string Environment { get; set; } = "Stage";
        public string MerchantId { get; set; } = string.Empty;
        public string HashKey { get; set; } = string.Empty;
        public string HashIV { get; set; } = string.Empty;

        public string PaymentActionUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string ClientBackUrl { get; set; } = string.Empty;
        public string QueryTradeInfoUrl { get; set; } = string.Empty;
    }
}
