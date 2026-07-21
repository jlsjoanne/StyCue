using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface IFashionQueryExpander
    {
        Task<SearchQueryTerms> ExpandAsync(
            string keyword, CancellationToken cancellationToken = default);
    }
}
