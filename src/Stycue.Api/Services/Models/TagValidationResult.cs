using Stycue.Api.Entities;

namespace Stycue.Api.Services.Models
{
    public class TagValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public List<Tag> Tags { get; set; } = [];
    }
}
