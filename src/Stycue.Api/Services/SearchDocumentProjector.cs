using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Enums;

namespace Stycue.Api.Services
{
    public class SearchDocumentProjector : ISearchDocumentProjector
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SearchDocumentProjector> _logger;

        private const int BackfillBatchSize = 100;

        public SearchDocumentProjector(AppDbContext dbContext, ILogger<SearchDocumentProjector> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task UpsertPostAsync(int postId, CancellationToken cancellationToken = default)
        {
            await UpsertPostCoreAsync(postId, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SearchDocument was upserted for Post {PostId}.",
                postId);
        }

        public async Task UpsertCommissionAsync(int commissionId, CancellationToken cancellationToken = default)
        {
            await UpsertCommissionCoreAsync(commissionId, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SearchDocument upserted for Commission {CommissionId}",
                commissionId);
        }

        public async Task HidePostAsync(int postId, CancellationToken cancellationToken = default)
        {
            var documentId = $"post-{postId}";

            await HideDocumentAsync(documentId, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SearchDocument {DocumentId} was hidden for Post {PostId}.",
                documentId, postId);
        }

        public async Task HideCommissionAsync(int commissionId, CancellationToken cancellationToken = default)
        {
            var documentId = $"commission-{commissionId}";

            await HideDocumentAsync(documentId, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SearchDocument {DocumentId} was hidden for Commission {CommissionId}.",
                documentId, commissionId);
        }

        public async Task BackfillAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("SearchDocument backfill started.");

            // 刻意包含 soft-deleted Post 與 Closed Commission，
            // 讓其既有投影也能被更新為 IsVisible = false。
            var postIds = await _dbContext.Posts.AsNoTracking()
                .OrderBy(p => p.Id).Select(p => p.Id).ToListAsync(cancellationToken);

            var commissionIds = await _dbContext.Commissions.AsNoTracking()
                .OrderBy(c => c.Id).Select(c => c.Id).ToListAsync(cancellationToken);

            var processedPostCount = 0;
            var processedCommissionCount = 0;

            foreach(var postBatch in postIds.Chunk(BackfillBatchSize))
            {
                foreach(var postId in postBatch)
                {
                    await UpsertPostCoreAsync(postId, cancellationToken);
                    processedPostCount++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }

            foreach(var commissionBatch in commissionIds.Chunk(BackfillBatchSize))
            {
                foreach(var commissionId in commissionBatch)
                {
                    await UpsertCommissionCoreAsync(commissionId, cancellationToken);
                    processedCommissionCount++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }

            await HideOrphanedDocumentsAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            _logger.LogInformation(
                "SearchDocument backfill completed. Posts: {PostCount}, Commissions: {CommissionCount}",
                processedPostCount, processedCommissionCount);
        }

        // private methods
        private async Task HideDocumentAsync(
            string documentId, CancellationToken cancellationToken)
        {
            var document = await _dbContext.SearchDocuments
                .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document == null)
            {
                return;
            }

            document.IsVisible = false;
            document.UpdatedAt = DateTime.UtcNow;
        }

        // 處理「來源已 hard delete、但投影殘留」的 helper
        private async Task HideOrphanedDocumentsAsync(CancellationToken cancellationToken)
        {
            var orphanedDocuments = await _dbContext.SearchDocuments
                .Where(document =>
                    (
                        (document.ItemType == HomepageItemType.PostShare || document.ItemType == HomepageItemType.PostAsk) &&
                        !_dbContext.Posts.Any(p => p.Id == document.ItemId)
                    ) ||
                    (
                        document.ItemType == HomepageItemType.Commission &&
                        !_dbContext.Commissions.Any(c => c.Id == document.ItemId)
                    ))
                .Where(document => document.IsVisible).ToListAsync(cancellationToken);

            foreach(var document in orphanedDocuments)
            {
                document.IsVisible = false;
                document.UpdatedAt = DateTime.UtcNow;
            }
        }

        // 「不儲存」的 private core method
        private async Task UpsertPostCoreAsync(int postId, CancellationToken cancellationToken)
        {
            var post = await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .SingleOrDefaultAsync(p => p.Id == postId, cancellationToken);

            var documentId = $"post-{postId}";

            // 來源已不存在時，不保留可能殘留的可見投影。
            if (post == null)
            {
                await HideDocumentAsync(documentId, cancellationToken);
                _logger.LogWarning("Search projection skipped because Post {PostId} was not found.",
                    postId);

                return;
            }

            var tagsText = post.PostTags != null ? string.Join(" ", post.PostTags
                .Select(pt => pt.Tag.Name?.Trim()).Where(name => !string.IsNullOrWhiteSpace(name)))
                : "";

            var document = await _dbContext.SearchDocuments
                .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document == null)
            {
                document = new SearchDocument
                {
                    Id = documentId
                };

                _dbContext.SearchDocuments.Add(document);
            }

            document.ItemType = post.PostType == PostType.Share
                ? HomepageItemType.PostShare : HomepageItemType.PostAsk;

            document.ItemId = post.Id;
            document.Title = post.Title;
            document.Content = post.Content;
            document.TagsText = tagsText;
            document.SearchText = string.Join(
                " ", new[] { post.Title, post.Content, tagsText }.Where(value => !string.IsNullOrWhiteSpace(value)));
            document.IsVisible = post.DeletedAt == null;
            document.UpdatedAt = post.UpdatedAt ?? post.DeletedAt ?? post.CreatedAt;
        }

        private async Task UpsertCommissionCoreAsync(int commissionId, CancellationToken cancellationToken)
        {
            var commission = await _dbContext.Commissions.AsNoTracking()
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .SingleOrDefaultAsync(c => c.Id == commissionId, cancellationToken);

            var documentId = $"commission-{commissionId}";

            if (commission == null)
            {
                await HideDocumentAsync(documentId, cancellationToken);
                _logger.LogWarning("Search projection skipped because Commission {CommissionId} was not found.",
                    commissionId);

                return;
            }

            var tagsText = commission.CommissionTags != null ? string.Join(" ", commission.CommissionTags
                .Select(pt => pt.Tag.Name?.Trim()).Where(name => !string.IsNullOrWhiteSpace(name)))
                : "";

            var document = await _dbContext.SearchDocuments
                .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            if (document == null)
            {
                document = new SearchDocument
                {
                    Id = documentId
                };

                _dbContext.SearchDocuments.Add(document);
            }

            document.ItemType = HomepageItemType.Commission;
            document.ItemId = commission.Id;
            document.Title = commission.Title;
            document.Content = commission.Content;
            document.TagsText = tagsText;
            document.SearchText = string.Join(
                " ", new[] { commission.Title, commission.Content, tagsText }.Where(value => !string.IsNullOrWhiteSpace(value)));
            document.IsVisible = commission.Status != CommissionStatus.Closed && commission.ClosedAt == null;
            document.UpdatedAt = commission.UpdatedAt ?? commission.ClosedAt ?? commission.CreatedAt;
        }
    }
}
