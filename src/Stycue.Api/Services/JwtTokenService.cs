using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.DTOs.Jwt;


namespace Stycue.Api.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IConfiguration _configuration;

        public JwtTokenService(IOptions<JwtOptions> jwtOptions, IConfiguration configuration)
        {
            _jwtOptions = jwtOptions.Value;
            _configuration = configuration;
        }

        public string GenerateAccessToken(JwtUserPayload jwtUserPayload)
        {
            // get secretKey from local: user-secrets / VM: environment variables
            var secretKey = _configuration["Jwt:SecretKey"];

            // input => claim
            var claims = new List<Claim>
            {
                new Claim("userId",jwtUserPayload.UserId.ToString()),
                new Claim("email", jwtUserPayload.Email),
                new Claim("nickName", jwtUserPayload.NickName),
                new Claim(ClaimTypes.Role, jwtUserPayload.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // set key to use
            var key = new SymmetricSecurityKey(Convert.FromBase64String(secretKey));

            // set credentials algorithms
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            // generate token
            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
