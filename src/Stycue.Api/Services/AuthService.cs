using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Auth;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Services.Models;
using Stycue.Api.Constants;

namespace Stycue.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IGoogleAuthService _googleAuthService;

        public AuthService(AppDbContext appDbContext, IPasswordService passwordService, IJwtTokenService jwtTokenService, IGoogleAuthService googleAuthService)
        {
            _dbContext = appDbContext;
            _passwordService = passwordService;
            _jwtTokenService = jwtTokenService;
            _googleAuthService = googleAuthService;
        }

        public async Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request)
        {
            // check Request data
            if (request == null)
            {
                return ApiResponse<RegisterResponse>.FailResult("註冊資料不可為空");
            }

            // 檢查密碼輸入是否一致 (非必要)
            if(request.Password != request.PasswordCheck)
            {
                return ApiResponse<RegisterResponse>.FailResult("密碼輸入不一致");
            }

            // 檢查 Email 是否已存在
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail);
            if (emailExists)
            {
                return ApiResponse<RegisterResponse>.FailResult("Email已被註冊");
            }

            // 建立 User
            var user = new User
            {
                Email = normalizedEmail,
                NickName = request.NickName.Trim(),
                Role = "User",
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            // 密碼加密: PasswordService.HashPassword
            user.PasswordHash = _passwordService.HashPassword(user, request.Password);

            // EF Core加入user
            _dbContext.Users.Add(user);

            // 寫入資料庫
            await _dbContext.SaveChangesAsync();

            // 回傳 RegisterResponse
            var response = new RegisterResponse
            {
                Email = user.Email,
                NickName = user.NickName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return ApiResponse<RegisterResponse>.SuccessResult(response,"成功註冊會員");
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            // check request input
            if(request == null)
            {
                return ApiResponse<LoginResponse>.FailResult("登入資料不可為空");
            }

            // 查詢User
            var normalizedEmail = request.Email.Trim().ToUpperInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            // 用Email查使用者是否已註冊
            if(user == null)
            {
                return ApiResponse<LoginResponse>.FailResult("帳號或密碼錯誤");
            }

            if(user.DeactivatedAt != null)
            {
                return ApiResponse<LoginResponse>.FailResult("此帳號目前無法使用，請聯繫客服或管理員", AuthErrorCodes.AccountDeactivated);
            }

            // 檢查使用者是否有 PasswordHash => 若沒有，則可能為Google註冊登入，回傳使用Google登入
            if(String.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return ApiResponse<LoginResponse>.FailResult("此帳號是使用 Google 登入建立，請使用 Google 登入");
            }

            // 檢查密碼是否正確
            var passwordResult = _passwordService.VerifyPassword(user, user.PasswordHash, request.Password);
            if(passwordResult == PasswordVerificationResult.Failed)
            {
                return ApiResponse<LoginResponse>.FailResult("帳號或密碼錯誤");
            }

            var jwtUserPayload = new JwtUserPayload
            {
                UserId = user.Id,
                Email = user.Email,
                NickName = user.NickName,
                Role = user.Role
            };

            var response = new LoginResponse
            {
                AccessToken = _jwtTokenService.GenerateAccessToken(jwtUserPayload),
                Email = user.Email,
                NickName = user.NickName,
                Role = user.Role
            };

            return ApiResponse<LoginResponse>.SuccessResult(response, "登入成功");
        }

        public async Task<ApiResponse<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request)
        {
            // check request
            if(request == null)
            {
                return ApiResponse<LoginResponse>.FailResult("Google 登入資料不可為空");
            }

            // check idToken
            if (String.IsNullOrWhiteSpace(request.IdToken))
            {
                return ApiResponse<LoginResponse>.FailResult("Google Token不可為空");
            }

            // call GoogleAuthService.ValidateIdTokenAsync
            var googlePayload = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);

            if(googlePayload == null)
            {
                return ApiResponse<LoginResponse>.FailResult("Google登入驗證失敗", AuthErrorCodes.GoogleTokenInvalid);
            }

            var normalizedEmail = googlePayload.Email.Trim().ToLowerInvariant();

            // 用GoogleSub查使用者
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleSub == googlePayload.GoogleSub);

            if(user == null)
            {
                user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            }

            if(user != null && user.DeactivatedAt != null)
            {
                return ApiResponse<LoginResponse>.FailResult("此帳號目前無法使用，請聯繫客服或管理員", AuthErrorCodes.AccountDeactivated);
            }

            if(user != null && String.IsNullOrWhiteSpace(user.GoogleSub))
            {
                user.GoogleSub = googlePayload.GoogleSub;
                user.IsEmailVerified = googlePayload.IsEmailVerified;
                user.UpdatedAt = DateTime.UtcNow;
            }

            if(user == null)
            {
                user = new User
                {
                    Email = normalizedEmail,
                    NickName = googlePayload.NickName ?? normalizedEmail.Split('@')[0],
                    GoogleSub = googlePayload.GoogleSub,
                    IsEmailVerified = googlePayload.IsEmailVerified,
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            // create JwtPayload
            var jwtUserPayload = new JwtUserPayload
            {
                UserId = user.Id,
                Email = user.Email,
                NickName = user.NickName,
                Role = user.Role
            };

            // generate response
            var response = new LoginResponse
            {
                AccessToken = _jwtTokenService.GenerateAccessToken(jwtUserPayload),
                Email = user.Email,
                NickName = user.NickName,
                Role = user.Role
            };

            return ApiResponse<LoginResponse>.SuccessResult(response, "Google登入成功");

        }
    }
}
