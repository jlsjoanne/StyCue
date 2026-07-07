namespace Stycue.Api.DTOs.Jwt
{
    public class JwtUserPayload
    {
        public int UserId { get; set; }

        public string Email { get; set; } = String.Empty;

        public string NickName { get; set; } = String.Empty;

        public string Role { get; set; } = String.Empty;
    }
}
