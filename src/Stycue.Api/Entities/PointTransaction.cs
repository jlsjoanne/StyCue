using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class PointTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int Amount { get; set; }
        public PointTransactionType TransactionType { get; set; }
        public PointReferenceType ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
