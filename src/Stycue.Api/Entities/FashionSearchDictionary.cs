using Stycue.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.Entities
{
    public class FashionSearchDictionary
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string CanonicalTerm { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Alias { get; set; } = string.Empty;
        public FashionSearchCategory Category { get; set; }
        public int Weight { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
