using Stycue.Api.Enums;

namespace Stycue.Api.Services.Models
{
    public sealed record HomepageCandidate(
        HomepageItemType ItemType,
        int ItemId,
        DateTime CreatedAt,
        DateTime EffectiveUpdatedAt,
        int CommentCount, int? CommissionPoints);
}
