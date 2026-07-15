using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Posts;

namespace Stycue.Api.Services.Interfaces
{
    public interface IPostService
    {
        Task<ApiResponse<PostDetailResponse>> CreateAsync(
            int userId, PostRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<PostDetailResponse>> GetPostAsync(
            int? userId, int postId, CancellationToken cancellationToken = default);

        Task<ApiResponse<PostDetailResponse>> UpdateAsync(
            int userId, int postId, PostRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<PostDeleteResponse>> DeleteAsync(
            int userId, int postId, CancellationToken cancellationToken = default);
    }
}
