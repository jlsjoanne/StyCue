using Stycue.Api.DTOs.Images;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Services
{
    public class ImageResponseBuilder : IImageResponseBuilder
    {
        private readonly IBlobStorageService _blobStorageService;

        public ImageResponseBuilder(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public ImageResponse Build(ImageAsset image)
        {
            return new ImageResponse{
                ImageId = image.Id,
                Purpose = image.Purpose,
                Url = _blobStorageService.GenerateReadSasUrl(image.BlobName),
                Category = image.FashionMetadata?.Category,
                Brand = image.FashionMetadata?.Brand
            };
        }

        public IReadOnlyList<ImageResponse> BuildList(IEnumerable<ImageAsset> images)
        {
            return images
                .Where(image => image.DeletedAt == null)
                .Select(Build)
                .ToList();
        }
    }
}
