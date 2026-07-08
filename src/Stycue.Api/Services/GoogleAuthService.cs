using Stycue.Api.Services.Models;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;


namespace Stycue.Api.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly GoogleAuthOptions _googleAuthOptions;

        public GoogleAuthService(IOptions<GoogleAuthOptions> googleAuthOptions)
        {
            _googleAuthOptions = googleAuthOptions.Value;
        }
        public async Task<GoogleUserPayload?> ValidateIdTokenAsync(string idToken)
        {
            // check idToken
            if (String.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }


            // check if ClientId set
            if (String.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
            {
                throw new InvalidOperationException("GoogleAuth:CliendId is missing.");
            }

            // use ClientId as Signature for Validation
            // ValidationSettings.Audience 是可信任的 client IDs
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleAuthOptions.ClientId]
            };

            // validation process
            try
            {
                // GoogleJsonWebSignature.ValidateAsync(jwt, validationSettings) 會驗證Google-issued JWT
                // 失敗會丟 InvalidJwtException，成功回傳 payload。
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

                // 檢查payload
                // sub是Google 帳號唯一識別值
                if (String.IsNullOrWhiteSpace(payload.Subject) || String.IsNullOrWhiteSpace(payload.Email))
                {
                    return null;
                }

                return new GoogleUserPayload
                {
                    GoogleSub = payload.Subject,
                    Email = payload.Email,
                    NickName = payload.Name,
                    IsEmailVerified = payload.EmailVerified
                };
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }
    }
}
