using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Payment;

namespace Stycue.Api.Services.Interfaces
{
    public interface IPointPurchaseService
    {
        Task<ApiResponse<List<PointProductResponse>>> GetProductsAsync(
            CancellationToken cancellationToken = default);

        Task<ApiResponse<CreatePointPurchaseResponse>> CreateAsync(
            int userId, CreatePointPurchaseRequest request, CancellationToken cancellationToken = default);

        Task<ApiResponse<PointPurchaseResponse>> GetAsync(
            int userId, int orderId, CancellationToken cancellationToken = default);

        Task<ApiResponse<object>> ProcessEcpayReturnAsync(
            EcpayPaymentReturnRequest request,
            IReadOnlyDictionary<string, string> formFields, CancellationToken cancellationToken = default);
    }
}
