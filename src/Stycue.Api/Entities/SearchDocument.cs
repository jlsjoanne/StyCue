using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class SearchDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; } = string.Empty;

        public HomepageItemType ItemType { get; set; }
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string TagsText { get; set; } = string.Empty;
        public string SearchText { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
