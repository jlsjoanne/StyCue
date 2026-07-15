using Stycue.Api.DTOs.Images;
using Stycue.Api.Entities;

namespace Stycue.Api.Services.Interfaces
{
    public interface IImageResponseBuilder
    {
        ImageResponse Build(ImageAsset image);

        IReadOnlyList<ImageResponse> BuildList(IEnumerable<ImageAsset> images);
    }
}
