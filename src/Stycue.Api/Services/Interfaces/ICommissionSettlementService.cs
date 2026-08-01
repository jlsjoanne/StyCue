using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface ICommissionSettlementService
    {
        Task<CommissionSettlementRunResult> RunOnceAsync(
            int batchSize, CancellationToken cancellationToken = default);
    }
}
