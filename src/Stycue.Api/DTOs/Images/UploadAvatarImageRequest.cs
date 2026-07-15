namespace Stycue.Api.DTOs.Images
{
    public class UploadAvatarImageRequest
    {
        /// <summary>
        /// 要上傳的大頭貼圖片檔案。
        /// 必須使用 multipart/form-data 上傳。僅接受 image/* 類型檔案。
        /// </summary>
        public IFormFile File { get; set; } = null!;
    }
}
