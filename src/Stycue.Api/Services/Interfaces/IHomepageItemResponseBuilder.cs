using Stycue.Api.DTOs.Homepage;
using Stycue.Api.Entities;

namespace Stycue.Api.Services.Interfaces
{
    public interface IHomepageItemResponseBuilder
    {
        HomepageItemResponse BuildPostItem(Post post, int? currentUserId);
        HomepageItemResponse BuildCommissionItem(Commission commission, int? currentUserId);
    }
}
