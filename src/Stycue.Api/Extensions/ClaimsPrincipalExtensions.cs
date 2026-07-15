using System.Security.Claims;

namespace Stycue.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        private const string UserIdClaimType = "userId";

        public static int? GetUserIdOrNull(this ClaimsPrincipal user)
        {
            var userIdValue = user.FindFirst(UserIdClaimType)?.Value;

            if(int.TryParse(userIdValue, out int userId))
            {
                return userId;
            }

            return null;
        }

        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userId = user.GetUserIdOrNull();

            if(userId == null)
            {
                throw new UnauthorizedAccessException("Invalid or missing userId claim.");
            }

            return userId.Value;
        }
    }
}
