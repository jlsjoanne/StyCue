using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Payment
{
    public class PointPurchaseResponse
    {
        public int OrderId { get; set; }
        public string MerchantTradeNo { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public int AmountTwd { get; set; }
        public int Points { get; set; }

        public PointPurchaseStatus Status { get; set; }
        public string? ProviderTradeNo { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
