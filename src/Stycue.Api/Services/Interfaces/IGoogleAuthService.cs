using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserPayload?> ValidateIdTokenAsync(string idToken);
    }
}
