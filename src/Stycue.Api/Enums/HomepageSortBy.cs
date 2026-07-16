namespace Stycue.Api.Enums
{
    /// <summary>
    /// 首頁列表排序方式
    /// </summary>
    public enum HomepageSortBy
    {
        /// <summary>
        /// 依建立時間由新到舊排序
        /// API 對外值：latest
        /// </summary>
        Latest = 1,
        /// <summary>
        /// 熱門委託排序，只回傳委託文，依委託積分由高到低排序
        /// API 對外值：highestCommissionPoints
        /// </summary>
        HighestCommissionPoints = 2,
        /// <summary>
        /// 依留言數由高到低排序
        /// API 對外值：mostComments
        /// </summary>
        MostComments = 3
    }
}
