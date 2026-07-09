namespace Stycue.Api.Entities
{
    public class CommissionRepost
    {
        public int Id { get; set; }
        public int CommissionId { get; set; }
        public Commission Commission { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string SupplementContent { get; set; } = String.Empty;
        public int AdditionalPoints { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ImageAsset> Images { get; set; } = new List<ImageAsset>();
    }
}
