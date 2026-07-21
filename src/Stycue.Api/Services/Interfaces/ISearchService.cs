using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Homepage;
using Stycue.Api.DTOs.Search;

namespace Stycue.Api.Services.Interfaces
{
    public interface ISearchService
    {
        Task<ApiResponse<PagedResponse<HomepageItemResponse>>> SearchAsync(
            int? userId, SearchQueryRequest request, CancellationToken cancellationToken);
    }
}
