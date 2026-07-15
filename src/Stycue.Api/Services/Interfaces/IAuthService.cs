using Stycue.Api.DTOs.Auth;
using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    }
}
