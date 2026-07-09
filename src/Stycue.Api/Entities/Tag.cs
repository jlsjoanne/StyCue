namespace Stycue.Api.Entities
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public ICollection<CommissionTag> CommissionTags { get; set; } = new List<CommissionTag>();
    }
}
