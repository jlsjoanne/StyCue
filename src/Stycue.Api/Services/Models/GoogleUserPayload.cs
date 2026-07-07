namespace Stycue.Api.Services.Models
{
    public class GoogleUserPayload
    {
        public string GoogleSub { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public string? NickName { get; set; }
        public bool IsEmailVerified { get; set; }
    }
}
