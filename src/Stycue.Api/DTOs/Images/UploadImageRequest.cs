using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Images
{
    public class UploadImageRequest
    {
        /// <summary>
        /// 要上傳的圖片檔案
        /// 必須使用 multipart/form-data 上傳。後端僅接受 image/* 類型檔案。
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// 圖片中的服飾分類
        /// 選填。若前端有提供，後端會建立 ImageFashionMetadata。
        /// Tops 上衣 = 1
        /// Bottoms 下身 = 2
        /// Shoes 鞋子 = 3
        /// Accessories 配件 = 4
        /// Bags 包包 = 5
        /// Outerwear 外套 = 6
        /// Dress 洋裝 = 7
        /// Other 其他 = 99
        /// </summary>
        public ImageCategory? Category { get; set; }

        /// <summary>
        /// 圖片中的品牌名稱
        /// 選填。若前端有提供，後端會建立 ImageFashionMetadata。
        /// </summary>
        public string? Brand { get; set; }
    }
}
