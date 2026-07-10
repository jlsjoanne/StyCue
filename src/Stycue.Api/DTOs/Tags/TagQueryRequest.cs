using Stycue.Api.Enums;

namespace Stycue.Api.DTOs.Tags
{
    public class TagQueryRequest
    {
        /// <summary>
        /// 標籤搜尋關鍵字
        /// 選填。提供時會依標籤名稱搜尋；未提供時依 Source 回傳熱門或常用標籤。
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 標籤分類篩選
        /// 選填。提供時只回傳指定分類的標籤。
        /// 
        /// 可用值：
        /// 1 = Occasion，場合
        /// 2 = Style，風格
        /// 3 = Season，季節
        /// 4 = Color，配色
        /// 5 = Fit，版型
        /// </summary>
        public TagCategory? TagCategory { get; set; }

        /// <summary>
        /// 標籤查詢來源
        /// 可用值：
        /// 1 = Search，依關鍵字搜尋
        /// 2 = Popular，熱門標籤
        /// 3 = MyFrequent，目前登入使用者常用標籤
        /// </summary>
        public TagQuerySource Source { get; set; } = TagQuerySource.Search;

        /// <summary>
        /// 回傳標籤數量上限，數量限制在1~50間
        /// 預設 20
        /// </summary>
        public int Limit { get; set; } = 20;
    }
}
