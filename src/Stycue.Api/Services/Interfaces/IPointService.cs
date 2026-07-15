using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Points;
using Stycue.Api.Enums;

namespace Stycue.Api.Services.Interfaces
{
    public interface IPointService
    {
        Task<ApiResponse<PointWalletResponse>> GetMyWalletAsync(
              int userId, CancellationToken cancellationToken = default);

        // ClaimDailyAsync 不建議直接呼叫一般 AddPointsAsync 後才建 DailyPointClaim
        // 因為每日領取要同時建立 DailyPointClaim 與 PointTransaction，且要處理唯一限制。
        Task<ApiResponse<DailyPointClaimResponse>> ClaimDailyAsync(
            int userId, CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResponse<PointTransactionResponse>>> GetTransactionsAsync(
            int userId, PointTransactionQueryRequest query, CancellationToken cancellationToken = default);

        Task<ApiResponse<PointWalletResponse>> GrantRegistrationRewardAsync(
            int userId, CancellationToken cancellationToken = default);

        Task<ApiResponse<PointWalletResponse>> AddPointsAsync(
            int userId, int amount, PointTransactionType transactionType, PointReferenceType referenceType,
            int? referenceId, string? description = null, CancellationToken cancellationToken = default);

        // SpendPointsAsync 要處理餘額不足
        Task<ApiResponse<PointWalletResponse>> SpendPointsAsync(
            int userId, int amount, PointTransactionType transactionType, PointReferenceType referenceType,
            int? referenceId, string? description = null, CancellationToken cancellationToken = default);
    }
}
