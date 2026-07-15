using AutoMapper;
using Stycue.Api.Entities;
using Stycue.Api.DTOs.Points;

namespace Stycue.Api.Mappings
{
    public class PointMappingProfile : Profile
    {
        public PointMappingProfile()
        {
            CreateMap<UserPointWallet, PointWalletResponse>();
            CreateMap<PointTransaction, PointTransactionResponse>();
        }
    }
}
