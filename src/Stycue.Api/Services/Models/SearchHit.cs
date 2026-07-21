using Stycue.Api.Enums;

namespace Stycue.Api.Services.Models
{
    public sealed record SearchHit(
        HomepageItemType ItemType, int ItemId, int Rank);
}
