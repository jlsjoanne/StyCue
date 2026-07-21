using AutoMapper;
using Stycue.Api.DTOs.Payment;
using Stycue.Api.Entities;

namespace Stycue.Api.Mappings
{
    public class PaymentMappingProfile : Profile
    {
        public PaymentMappingProfile()
        {
            CreateMap<PointProduct, PointProductResponse>();

            CreateMap<PointPurchaseOrder, PointPurchaseResponse>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.PointProduct.Name));
        }
    }
}
