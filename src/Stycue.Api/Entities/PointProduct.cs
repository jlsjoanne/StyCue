using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.Entities
{
    public class PointProduct
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Range(1,int.MaxValue)]
        public int PriceTwd { get; set; }

        [Range(1, int.MaxValue)]
        public int Points { get; set; }

        [Range(0, int.MaxValue)]
        public int BasePoints { get; set; }

        [Range(0, int.MaxValue)]
        public int BonusPoints { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }
    }
}
