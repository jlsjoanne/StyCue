namespace Stycue.Api.Services.Interfaces
{
    public interface ISearchDocumentProjector
    {
        Task UpsertPostAsync(int postId, CancellationToken cancellationToken = default);
        Task UpsertCommissionAsync(int commissionId, CancellationToken cancellationToken = default);
        Task HidePostAsync(int postId, CancellationToken cancellationToken = default);
        Task HideCommissionAsync(int commissionId, CancellationToken cancellationToken = default);
        Task BackfillAsync(CancellationToken cancellationToken = default);
    }
}
