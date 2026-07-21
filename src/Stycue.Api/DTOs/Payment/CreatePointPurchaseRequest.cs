using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Payment
{
    public class CreatePointPurchaseRequest
    {
        [Range(1, int.MaxValue)]
        public int PointProductId { get; set; }
    }
}
