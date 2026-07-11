using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.Extensions
{
    public static class OwnershipGuard
    {
        public static bool IsOwner(int ownerUserId, int? userId)
        {
            return userId.HasValue && ownerUserId == userId;
        }

        public static ApiResponse<T>? EnsureOwner<T>(
            int ownerUserId, int currentUserId, string resourceName = "資源")
        {
            if(ownerUserId == currentUserId)
            {
                return null;
            }

            return ApiResponse<T>.FailResult($"沒有權限操作此{resourceName}", "FORBIDDEN");
        }
    }
}
