namespace Stycue.Api.Services.Models
{
    public class BlobUploadResult
    {
        public string BlobName { get; set; } = String.Empty;
        public string ContainerName { get; set; } = String.Empty;
        public string Url { get; set; } = String.Empty;
        public string ContentType { get; set; } = String.Empty;
    }
}
