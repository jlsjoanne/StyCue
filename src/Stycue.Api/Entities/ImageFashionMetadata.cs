using Stycue.Api.Enums;

namespace Stycue.Api.Entities
{
    public class ImageFashionMetadata
    {
        public int ImageAssetId { get; set; }
        public ImageAsset ImageAsset { get; set; } = null!;
        public ImageCategory Category { get; set; }
        public string? Brand { get; set; }
    }

}
