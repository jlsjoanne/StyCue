using Stycue.Api.DTOs.Comm;
using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface ISearchCandidateProvider
    {
        Task<PagedResponse<SearchHit>> FindCandidatesAsync(
            SearchQueryTerms terms,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
