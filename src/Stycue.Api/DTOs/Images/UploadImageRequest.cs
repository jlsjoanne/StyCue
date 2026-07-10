using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Images
{
    public class UploadImageRequest
    {
        /// <summary>
        /// 要上傳的圖片檔案
        /// </summary>
        /// <remarks>
        /// 必須使用 multipart/form-data 上傳。後端僅接受 image/* 類型檔案。
        /// </remarks>
        public IFormFile File { get; set; } = null!;
        /// <summary>
        /// 圖片中的服飾分類
        /// </summary>
        /// <remarks>
        /// 選填。若前端有提供，後端會建立 ImageFashionMetadata。
        /// </remarks>
        public ImageCategory? Category { get; set; }
        /// <summary>
        /// 圖片中的品牌名稱
        /// </summary>
        /// <remarks>
        /// 選填。若前端有提供，後端會建立 ImageFashionMetadata。
        /// </remarks>
        public string? Brand { get; set; }
    }
}
