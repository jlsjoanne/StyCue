namespace Stycue.Api.Enums
{
    /// <summary>
    /// 標籤查詢來源
    /// </summary>
    public enum TagQuerySource
    {
        /// <summary>
        /// 依關鍵字搜尋標籤
        /// </summary>
        Search = 1,
        /// <summary>
        /// 查詢熱門標籤，依貼文與委託文使用次數排序
        /// </summary>
        Popular = 2,
        /// <summary>
        /// 查詢目前登入使用者常用標籤，依該使用者在貼文與委託文中的使用次數排序
        /// </summary>
        MyFrequent = 3
    }
}
