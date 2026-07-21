namespace Stycue.Api.DTOs.Payment
{
    public class PointProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PriceTwd { get; set; }
        public int BasePoints { get; set; }
        public int BonusPoints { get; set; }
        public int Points { get; set; }
    }
}
