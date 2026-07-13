using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Comments;

namespace Stycue.Api.Services.Interfaces
{
    public interface ICommentService
    {
        // 取得委託文留言列表
        Task<ApiResponse<List<CommentResponse>>> GetCommissionCommentsAsync(
            int? userId, int commissionId, CancellationToken cancellationToken = default);

        // 建立委託文根留言
        Task<ApiResponse<CommentResponse>> CreateForCommissionAsync(
            int userId, int commissionId, CreateCommentRequest request, CancellationToken cancellationToken = default);

        // 取得貼文留言列表
        Task<ApiResponse<List<CommentResponse>>> GetPostCommentsAsync(
            int? userId, int postId, CancellationToken cancellationToken = default);

        // 建立貼文根留言
        Task<ApiResponse<CommentResponse>> CreateForPostAsync(
            int userId, int postId, CreateCommentRequest request, CancellationToken cancellationToken = default);

        // 回覆留言
        Task<ApiResponse<CommentResponse>> ReplyAsync(
            int userId, int parentCommentId, CreateCommentRequest request, CancellationToken cancellationToken = default);

        // 編輯留言
        Task<ApiResponse<CommentResponse>> UpdateAsync(
            int userId, int commentId, UpdateCommentRequest request, CancellationToken cancellationToken = default);

        // 刪除留言
        Task<ApiResponse<object>> DeleteAsync(
            int userId, int commentId, CancellationToken cancellationToken);
    }
}
