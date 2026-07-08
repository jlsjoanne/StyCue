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
        /// 回傳資料
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 成功回應
        /// </summary>
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
        /// 失敗回應
        /// </summary>
        /// <param name="message"></param>
        public static ApiResponse<T> FailResult(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}
