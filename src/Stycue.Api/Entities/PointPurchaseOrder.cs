using System.ComponentModel.DataAnnotations;
using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class PointPurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string MerchantTradeNo { get; set; } = string.Empty;


        public int UserId { get; set; }

        public User User { get; set; } = null!;


        public int PointProductId { get; set; }
        public PointProduct PointProduct { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int AmountTwd { get; set; }

        [Range(1, int.MaxValue)]
        public int Points { get; set; }
        public PaymentProvider PaymentProvider { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PointPurchaseStatus Status { get; set; }

        [MaxLength(64)]
        public string? ProviderTradeNo { get; set; }

        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        //  兩個 callback 同時處理同一張 Pending 訂單時，只有一個更新能成功，避免重複入帳
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
    }
}
