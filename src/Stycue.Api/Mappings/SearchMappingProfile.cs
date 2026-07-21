using AutoMapper;
using Stycue.Api.Entities;
using Stycue.Api.DTOs.SearchHistory;

namespace Stycue.Api.Mappings
{
    public class SearchMappingProfile : Profile
    {
        public SearchMappingProfile()
        {
            CreateMap<SearchHistory, SearchHistoryResponse>();
        }
    }
}
