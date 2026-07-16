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

        public UserSummaryResponse Build(User user, int? currentUserId, ISet<int> followedUserIds)
        {
            var response = Build(user);

            response.IsFollowing = ResolveIsFollowing(user.Id, currentUserId, followedUserIds);

            return response;
        }

        public IReadOnlyList<UserSummaryResponse> BuildList(IEnumerable<User> users)
        {
            return users.Select(Build).ToList();
        }

        public IReadOnlyList<UserSummaryResponse> BuildList(
            IEnumerable<User> users, int? currentUserId, ISet<int> followedUserIds)
        {
            return users.Select(user => Build(user, currentUserId, followedUserIds)).ToList();
        }

        private static bool? ResolveIsFollowing(int targetUserId, int? currentUserId, ISet<int> followedUserIds)
        {
            if (!currentUserId.HasValue)
            {
                return null;
            }

            if(targetUserId == currentUserId.Value)
            {
                return null;
            }

            return followedUserIds.Contains(targetUserId);
        }
    }
}
