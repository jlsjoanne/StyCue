namespace Stycue.Api.DTOs.Payment
{
    public class EcpayPaymentReturnRequest
    {
        public string MerchantID { get; set; } = string.Empty;
        public string MerchantTradeNo { get; set; } = string.Empty;

        public int RtnCode { get; set; }
        public string RtnMsg { get; set; } = string.Empty;

        public string TradeNo { get; set; } = string.Empty;
        public int TradeAmt { get; set; }

        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string CheckMacValue { get; set; } = string.Empty;

        public int? SimulatePaid { get; set; }
    }
}
