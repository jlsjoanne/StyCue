namespace Stycue.Api.DTOs.Payment
{
    public class EcpayQueryTradeInfoResponse
    {
        public int TradeStatus { get; set; }
        public string MerchantTradeNo { get; set; } = string.Empty;
        public string TradeNo { get; set; } = string.Empty;
        public int TradeAmt { get; set; }
    }
}
