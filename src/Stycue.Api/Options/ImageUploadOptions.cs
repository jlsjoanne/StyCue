namespace Stycue.Api.Options
{
    public class ImageUploadOptions
    {
        public int DailyMaxImageCount { get; init; } = 20;
        public long DailyMaxTotalBytes { get; init; } = 20L * 1024 * 1024;
    }
}
