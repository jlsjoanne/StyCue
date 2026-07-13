using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Likes;

namespace Stycue.Api.Services.Interfaces
{
    public interface ILikeService
    {
        // 留言按讚
        Task<ApiResponse<LikeResponse>> LikeCommentAsync(
            int userId, int commentId, CancellationToken cancellationToken);

        // 留言取消按讚
        Task<ApiResponse<LikeResponse>> UnlikeCommentAsync(
            int userId, int commentId, CancellationToken cancellationToken);

        // 委託文按讚
        Task<ApiResponse<LikeResponse>> LikeCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken);

        // 委託文取消按讚
        Task<ApiResponse<LikeResponse>> UnlikeCommissionAsync(
            int userId, int commissionId, CancellationToken cancellationToken);

        // 貼文按讚
        Task<ApiResponse<LikeResponse>> LikePostAsync(
            int userId, int postId, CancellationToken cancellationToken);

        // 貼文取消按讚
        Task<ApiResponse<LikeResponse>> UnlikePostAsync(
            int userId, int postId, CancellationToken cancellationToken);
    }
}
