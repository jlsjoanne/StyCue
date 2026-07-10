using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Tags
{
    /// <summary>
    /// 建立或取得標籤的 request
    /// </summary>
    /// <remarks>
    /// 前端可一次送出多個標籤。後端會逐一判斷標籤是否已存在；
    /// 已存在則回傳既有標籤，不存在則建立新標籤。
    /// </remarks>
    public class CreateTagRequest
    {
        /// <summary>
        /// 使用者選擇或輸入的標籤清單
        /// </summary>
        /// <remarks>
        /// 至少需包含一個標籤。每個標籤可帶自己的分類。
        /// </remarks>
        public List<CreateTagItemRequest> Tags { get; set; } = new();
    }

    /// <summary>
    /// 單一建立或取得標籤項目
    /// </summary>
    public class CreateTagItemRequest
    {
        /// <summary>
        /// 標籤顯示名稱
        /// </summary>
        /// <remarks>
        /// 後端會進行空白整理與正規化。若正規化後的標籤已存在，會回傳既有標籤。
        /// </remarks>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 標籤分類
        /// </summary>
        /// <remarks>
        /// 選填。可用於區分場合、風格、季節、配色、版型等前端分類。
        ///
        /// 可用值：
        /// 1 = Occasion，場合
        /// 2 = Style，風格
        /// 3 = Season，季節
        /// 4 = Color，配色
        /// 5 = Fit，版型
        /// </remarks>
        public TagCategory? TagCategory { get; set; }
    }
}
