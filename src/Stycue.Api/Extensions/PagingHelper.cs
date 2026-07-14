namespace Stycue.Api.Extensions
{
    public static class PagingHelper
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public static (int Page, int PageSize) Normalize(int page, int pageSize)
        {
            var normalizedPage = page < 1 ? DefaultPage : page;
            var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

            return (normalizedPage, normalizedPageSize);
        }

        public static int CalculateTotalPages(int totalCount, int pageSize)
        {
            return totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        }
    }
}
