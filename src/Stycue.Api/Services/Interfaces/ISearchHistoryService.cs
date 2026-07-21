using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.SearchHistory;

namespace Stycue.Api.Services.Interfaces
{
    public interface ISearchHistoryService
    {
        Task<ApiResponse<List<SearchHistoryResponse>>> GetMyHistoryAsync(
            int userId, SearchHistoryQueryRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<object>> ClearMyHistoryAsync(
            int userId, CancellationToken cancellationToken = default);

        Task RecordSuccessfulSearchAsync(
            int userId, string keyword, CancellationToken cancellationToken = default);
    }
}
