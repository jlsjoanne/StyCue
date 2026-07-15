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
        /// 依按讚數由高到低排序
        /// API 對外值：mostLikes
        /// </summary>
        MostLikes = 2,
        /// <summary>
        /// 依留言數由高到低排序
        /// API 對外值：mostComments
        /// </summary>
        MostComments = 3
    }
}
