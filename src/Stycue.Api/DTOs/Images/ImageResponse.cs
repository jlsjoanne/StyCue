using System.Text.Json.Serialization;
using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Images
{
    public class ImageResponse
    {
        /// <summary>
        /// 圖片資料 ID
        /// </summary>
        public int ImageId { get; set; }

        /// <summary>
        /// 圖片用途
        /// </summary>
        /// <remarks>
        /// 可能值包含 Profile、Post、Commission、Comment。
        /// </remarks>
        public string Purpose { get; set; } = String.Empty;

        /// <summary>
        /// 圖片顯示用 URL
        /// </summary>
        /// <remarks>
        /// 此 URL 為短效 Read-only SAS URL，前端可直接用於 img src。SAS URL 不會存入資料庫，過期後需重新查詢 API 取得新 URL。
        /// </remarks>
        public string Url { get; set; } = String.Empty;

        /// <summary>
        /// 圖片中的服飾分類
        /// </summary>
        /// <remarks>
        /// 未提供時不回傳此欄位。
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImageCategory? Category { get; set; }

        /// <summary>
        /// 圖片中的品牌名稱
        /// </summary>
        /// <remarks>
        /// 未提供時不回傳此欄位。
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Brand { get; set; }
    }
}
