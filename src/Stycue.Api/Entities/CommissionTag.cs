namespace Stycue.Api.Entities
{
    public class CommissionTag
    {
        public int CommissionId { get; set; }
        public Commission Commission { get; set; } = null!;
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
