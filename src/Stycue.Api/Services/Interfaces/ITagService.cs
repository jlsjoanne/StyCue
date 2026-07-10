using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Entities;

namespace Stycue.Api.Services.Interfaces
{
    public interface ITagService
    {
        Task<ApiResponse<List<TagResponse>>> GetTagsAsync(
            int? userId, TagQueryRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<List<TagResponse>>> CreateOrGetAsync(
            CreateTagRequest request, CancellationToken cancellationToken = default);

        Task<List<Tag>> ValidateTagIdsAsync(
            IEnumerable<int> tagIds, CancellationToken cancellationToken = default);
    }
}
