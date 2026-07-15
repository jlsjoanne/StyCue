using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;

namespace Stycue.Api.Services.Interfaces
{
    public interface IHomepageService
    {
        Task<ApiResponse<PagedResponse<HomepageItemResponse>>> GetHomepageAsync(
            int? userId, HomepageQueryRequest request, CancellationToken cancellationToken);
    }
}
