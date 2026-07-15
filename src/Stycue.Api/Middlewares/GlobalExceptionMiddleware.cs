using System.Net;
using Stycue.Api.DTOs.Comm;

namespace Stycue.Api.Middlewares
{
    // 把warning跟error只在後端log，前端回傳固定格式JSON
    public class GlobalExceptionMiddleware
    {
        // 把 request 傳給下一個 middleware，並讓 GlobalExceptionMiddleware 包住後續流程。
        private readonly RequestDelegate _next;
        // 把 exception 記錄到後端 log，方便除錯，不直接給前端。
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        // 判斷 Development / Production，決定前端錯誤訊息要多詳細。
        // Development: 可以回傳較詳細錯誤訊息，方便開發除錯。
        // Production: 只回傳通用錯誤訊息，避免洩漏資料庫欄位、檔案路徑、Blob connection、stack trace 等敏感資訊。
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                // LogWarning => 把警告記到後端 log
                // context.Request.Path => 呼叫的API
                _logger.LogWarning(ex, "Unauthorized access. Path: {Path}", context.Request.Path);

                await WriteErrorResponseAsync(
                    context, StatusCodes.Status401Unauthorized,
                    "未登入或登入資訊無效", "UNAUTHORIZED");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation. Path: {Path}", context.Request.Path);

                var message = _environment.IsDevelopment() ? ex.Message : "操作無效";

                await WriteErrorResponseAsync(
                    context, StatusCodes.Status400BadRequest,
                    message, "INVALID_OPERATION");
            }
            catch (Exception ex)
            {
                // LogError: 把錯誤記到後端 log
                _logger.LogError(ex, "Unhandled exception. Path: {Path}", context.Request.Path);

                var message = _environment.IsDevelopment() ? ex.Message : "伺服器發生錯誤";

                await WriteErrorResponseAsync(
                    context, StatusCodes.Status500InternalServerError,
                    message, "INTERNAL_SERVER_ERROR");
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message, string errorCode)
        {
            // 保護措施
            // 如果 response 已經開始送出了，就不要再嘗試改成錯誤 JSON
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = ApiResponse<object>.FailResult(message, errorCode);

            await context.Response.WriteAsJsonAsync(response);
        }

    }
}
