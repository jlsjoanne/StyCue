using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stycue.Api.DTOs.Auth;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 使用者註冊
        /// </summary>
        /// <param name="request">註冊資料</param>
        /// <returns>註冊成功後的使用者基本資料</returns>
        /// <response code="200">註冊成功</response>
        /// <response code="400">註冊資料錯誤或 Email 已被註冊</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        /// <param name="request">登入資料</param>
        /// <returns>登入成功後的 JWT Access Token 與使用者基本資料</returns>
        /// <response code="200">登入成功</response>
        /// <response code="401">帳號或密碼錯誤</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Google 登入或註冊
        /// </summary>
        /// <param name="request">Google ID Token</param>
        /// <returns>Google 登入成功後的 JWT Access Token 與使用者基本資料</returns>
        /// <response code="200">Google 登入成功</response>
        /// <response code="401">Google Token 驗證失敗</response>
        [HttpPost("google-login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            var result = await _authService.GoogleLoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
    }
}
