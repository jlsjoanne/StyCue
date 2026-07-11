namespace Stycue.Api.Enums
{
    /// <summary>
    /// 首頁列表篩選類型
    /// </summary>
    public enum HomepageFilter
    {
        /// <summary>
        /// 顯示所有分享文、提問文與委託文
        /// API 對外值：all
        /// </summary>
        All = 0,
        /// <summary>
        /// 只顯示分享文
        /// API 對外值：postShare
        /// </summary>
        PostShare = 1,
        /// <summary>
        /// 只顯示提問文
        /// API 對外值：postAsk
        /// </summary>
        PostAsk = 2,
        /// <summary>
        /// 只顯示委託文
        /// API 對外值：commission
        /// </summary>
        Commission = 3
    }
}
