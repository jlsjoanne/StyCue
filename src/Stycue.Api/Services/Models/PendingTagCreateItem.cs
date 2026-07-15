using Stycue.Api.Enums;

namespace Stycue.Api.Services.Models
{
    public class PendingTagCreateItem
    {
        public string OriginalName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public TagCategory? TagCategory { get; set; }

        public int Order { get; set; }
    }
}
