using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Commissions;

namespace Stycue.Api.Services.Interfaces
{
    public interface ICommissionService
    {
        // 取得委託文內容
        Task<ApiResponse<CommissionDetailResponse>> GetCommissionAsync(
              int? userId,
              int commissionId,
              CancellationToken cancellationToken = default);

        // 建立委託
        Task<ApiResponse<CommissionDetailResponse>> CreateAsync(
            int userId,
            CreateCommissionRequest request,
            CancellationToken cancellationToken = default);

        // 提前關閉委託
        Task<ApiResponse<CloseCommissionResponse>> CloseAsync(
            int userId,
            int commissionId,
            CancellationToken cancellationToken = default);

        // 到期後補充內容並重新開啟委託
        Task<ApiResponse<CommissionDetailResponse>> RepostAsync(
            int userId,
            int commissionId,
            RepostCommissionRequest request,
            CancellationToken cancellationToken = default);

        // 加碼委託文積分並延長到期時間
        Task<ApiResponse<BoostCommissionResponse>> BoostAsync(
            int userId,
            int commissionId,
            BoostCommissionRequest request,
            CancellationToken cancellationToken = default);

        // 委託者手動選擇最佳留言並發放積分
        Task<ApiResponse<CommissionRewardResponse>> SelectBestCommentAsync(
            int userId,
            int commissionId,
            SelectBestCommentRequest request,
            CancellationToken cancellationToken = default);

        // 委託文到期後結算獎勵
        //Task<ApiResponse<SettleRewardResponse>> SettleRewardAsync(
        //    int userId,
        //    int commissionId,
        //    CancellationToken cancellationToken = default);
    }
}
