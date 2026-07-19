using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Services.Models;
using Stycue.Api.Extensions;

namespace Stycue.Api.Services
{
    public class MvpSearchCandidateProvider : ISearchCandidateProvider
    {
        private readonly AppDbContext _dbContext;

        private const int MaxCandidateDocuments = 500;

        public MvpSearchCandidateProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResponse<SearchHit>> FindCandidatesAsync(
            SearchQueryTerms terms,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new ArgumentException("place holder");
        }

        
    }
}
