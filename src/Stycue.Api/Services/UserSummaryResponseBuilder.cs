using AutoMapper;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Services
{
    public class UserSummaryResponseBuilder : IUserSummaryResponseBuilder
    {
        private readonly IMapper _mapper;
        private readonly IBlobStorageService _blobStorageService;

        public UserSummaryResponseBuilder(IMapper mapper, IBlobStorageService blobStorageService)
        {
            _mapper = mapper;
            _blobStorageService = blobStorageService;
        }

        public UserSummaryResponse Build(User user)
        {
            var response = _mapper.Map<UserSummaryResponse>(user);

            if( user.AvatarImage != null && user.AvatarImage.DeletedAt == null)
            {
                response.AvatarUrl = _blobStorageService.GenerateReadSasUrl(user.AvatarImage.BlobName);
            }

            return response;
        }

        public IReadOnlyList<UserSummaryResponse> BuildList(IEnumerable<User> users)
        {
            return users.Select(Build).ToList();
        }
    }
}
