using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Comm
{
    /// <summary>
    /// API統一回應格式
    /// </summary>
    /// <typeparam name="T">回應資料型別</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Request是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 回傳訊息
        /// </summary>
        public string Message { get; set; } = String.Empty;

        /// <summary>
        /// 取得或設定成功時回傳的資料；失敗時通常為 null。
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 取得或設定失敗時的機器可讀錯誤代碼；成功或無特定錯誤代碼時不回傳。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        /// <param name="data">回應資料。</param>
        /// <param name="message">成功訊息。</param>
        /// <returns>成功的 API 回應。</returns>
        public static ApiResponse<T> SuccessResult(T data, string message = "請求成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// 建立失敗回應
        /// </summary>
        /// <param name="message">失敗訊息。</param>
        /// <param name="errorCode">機器可讀錯誤代碼，供前端或 Controller 判斷錯誤類型。</param>
        /// <returns>失敗的 API 回應。</returns>
        public static ApiResponse<T> FailResult(string message, string? errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                ErrorCode = errorCode
            };
        }
    }
}
