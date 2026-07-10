using Stycue.Api.Enums;
using System.Text.Json.Serialization;

namespace Stycue.Api.DTOs.Tags
{
    public class TagResponse
    {
        /// <summary>
        /// 標籤 ID
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// 標籤顯示名稱
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// 標籤分類
        /// 未分類標籤不回傳此欄位。
        /// 1 = Occasion，場合
        /// 2 = Style，風格
        /// 3 = Season，季節
        /// 4 = Color，配色
        /// 5 = Fit，版型
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TagCategory? TagCategory { get; set; }

        /// <summary>
        /// 標籤使用次數
        /// 熱門標籤或常用標籤查詢時回傳；一般建立或搜尋結果可不回傳。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? UsageCount { get; set; }
    }
}
