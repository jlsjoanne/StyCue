using Stycue.Api.DTOs.Comm;
using Stycue.Api.Entities;

namespace Stycue.Api.Services.Interfaces
{
    public interface IUserSummaryResponseBuilder
    {
        UserSummaryResponse Build(User user);

        UserSummaryResponse Build(User user, int? currentUserId, ISet<int> followedUserIds);

        IReadOnlyList<UserSummaryResponse> BuildList(IEnumerable<User> users);

        IReadOnlyList<UserSummaryResponse> BuildList(
            IEnumerable<User> users, int? currentUserId, ISet<int> followedUserIds);
    }
}
