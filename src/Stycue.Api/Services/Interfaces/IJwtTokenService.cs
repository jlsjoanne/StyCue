using System.Security.Claims;
using Stycue.Api.DTOs.Jwt;

namespace Stycue.Api.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(JwtUserPayload jwtUserPayload);
    }
}
