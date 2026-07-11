using Stycue.Api.DTOs.Comm;
using Stycue.Api.Entities;

namespace Stycue.Api.Services.Interfaces
{
    public interface IUserSummaryResponseBuilder
    {
        UserSummaryResponse Build(User user);

        IReadOnlyList<UserSummaryResponse> BuildList(IEnumerable<User> users);
    }
}
