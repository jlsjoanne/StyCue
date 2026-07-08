using System.Security.Claims;
using Stycue.Api.Services.Models;

namespace Stycue.Api.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(JwtUserPayload jwtUserPayload);
    }
}
