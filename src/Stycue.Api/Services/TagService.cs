using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.Entities;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Services.Models;
using Stycue.Api.Enums;

namespace Stycue.Api.Services
{
    public class TagService : ITagService
    {
        private readonly AppDbContext _dbContext;

        public TagService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<List<TagResponse>>> GetTagsAsync(
            int? userId, TagQueryRequest? request, CancellationToken cancellationToken = default)
        {
            var inputFilter = request ?? new TagQueryRequest();

            if( !Enum.IsDefined(typeof(TagQuerySource), inputFilter.Source))
            {
                return ApiResponse<List<TagResponse>>.FailResult("不合法的標籤查詢來源", "INVALID_TAG_QUERY_SOURCE");
            }

            if(!IsValidTagCategory(inputFilter.TagCategory))
            {
                return ApiResponse<List<TagResponse>>.FailResult("不合法的標籤分類", "INVALID_TAG_CATEGORY");
            }

            var limit = Math.Clamp(inputFilter.Limit, 1, 50);

            var query = _dbContext.Tags.AsNoTracking();

            if (inputFilter.TagCategory.HasValue)
            {
                query = query.Where(t => t.TagCategory == inputFilter.TagCategory.Value);
            }

            switch (inputFilter.Source)
            {
                case TagQuerySource.Search:
                    var keyword = NormalizeKey(inputFilter.Keyword ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        return ApiResponse<List<TagResponse>>.SuccessResult(new List<TagResponse>(), "標籤查詢成功");
                    }

                    var searchTags = await query.Where(t => t.NormalizedName.Contains(keyword))
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(limit).ToListAsync(cancellationToken);

                    var searchResponse = searchTags.Select(t => MapToResponse(t)).ToList();

                    return ApiResponse<List<TagResponse>>.SuccessResult(searchResponse, "標籤查詢成功");
                case TagQuerySource.Popular:
                    var populaTags = await query.Select(t => new
                    {
                        Tag = t,
                        UsageCount = t.PostTags.Count + t.CommissionTags.Count
                    }).Where(t => t.UsageCount > 0)
                    .OrderByDescending(t => t.UsageCount)
                    .ThenBy(x => x.Tag.Name)
                    .Take(limit).ToListAsync(cancellationToken);

                    var popularResponse = populaTags.Select(t => MapToResponse(t.Tag, t.UsageCount)).ToList();
                    return ApiResponse<List<TagResponse>>.SuccessResult(popularResponse, "標籤查詢成功");
                case TagQuerySource.MyFrequent:
                    if(userId == null)
                    {
                        return ApiResponse<List<TagResponse>>.FailResult("請先登入", "LOGIN_REQUIRED");
                    }

                    var myTags = await query.Select(t => new
                    {
                        Tag = t,
                        UsageCount = t.PostTags.Count(pt => pt.Post.UserId == userId.Value) + t.CommissionTags.Count(ct => ct.Commission.UserId == userId.Value)
                    })
                        .Where(t => t.UsageCount > 0)
                        .OrderByDescending(t => t.UsageCount)
                        .ThenBy(t => t.Tag.Name)
                        .Take(limit)
                        .ToListAsync(cancellationToken);

                    var myResponse = myTags.Select(t => MapToResponse(t.Tag, t.UsageCount)).ToList();
                    return ApiResponse<List<TagResponse>>.SuccessResult(myResponse, "標籤查詢成功");
                default:
                    return ApiResponse<List<TagResponse>>.FailResult("不合法的標籤查詢來源", "INVALID_TAG_QUERY_SOURCE");
            }
        }

        public async Task<ApiResponse<List<TagResponse>>> CreateOrGetAsync(
            CreateTagRequest request, CancellationToken cancellationToken = default)
        {
            if(request.Tags == null || !request.Tags.Any())
            {
                return ApiResponse<List<TagResponse>>.FailResult("請至少提供一個標籤", "TAG_REQUIRED");
            }

            var pendingTags = new List<PendingTagCreateItem>();

            foreach(var item in request.Tags)
            {
                if(!IsValidTagCategory(item.TagCategory))
                {
                    return ApiResponse<List<TagResponse>>.FailResult("不合法的標籤分類", "INVALID_TAG_CATEGORY");
                }
                
                var originalName = item.Name;
                var displayName = NormalizeDisplayName(originalName);

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    return ApiResponse<List<TagResponse>>.FailResult("標籤名稱不可為空", "INVALID_TAG_NAME");
                }

                var normalizedName = displayName.ToLowerInvariant();

                var alreadyExistsInRequest = pendingTags.Any(t => t.NormalizedName == normalizedName);

                if (alreadyExistsInRequest)
                {
                    continue;
                }

                pendingTags.Add(new PendingTagCreateItem
                {
                    OriginalName = originalName,
                    DisplayName = displayName,
                    NormalizedName = normalizedName,
                    TagCategory = item.TagCategory,
                    Order = pendingTags.Count
                });
            }

            
            var normalizedNames = pendingTags.Select(t => t.NormalizedName).ToList();

            // get existing tags from db
            var existingTags = await _dbContext.Tags
                .Where(t => normalizedNames.Contains(t.NormalizedName)).ToListAsync(cancellationToken);

            // get existing name for filter them out from pending tags
            var existingNames = existingTags.Select(t => t.NormalizedName).ToHashSet();

            // get new tags that need to be added
            var newTags = pendingTags.Where(t => !existingNames.Contains(t.NormalizedName))
               .Select(t => new Tag
               {
                   Name = t.DisplayName,
                   NormalizedName = t.NormalizedName,
                   TagCategory = t.TagCategory
               }).ToList();

            // if there is any new tag, add to db
            if (newTags.Any())
            {
                _dbContext.Tags.AddRange(newTags);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var tagsByNormalizedName = existingTags
                .Concat(newTags).ToDictionary(x => x.NormalizedName);

            var response = pendingTags
                .OrderBy(t => t.Order)
                .Select(x => MapToResponse(tagsByNormalizedName[x.NormalizedName]))
                .ToList();

            return ApiResponse<List<TagResponse>>.SuccessResult(response, "標籤建立或取得成功");
        }

        public async Task<List<Tag>> ValidateTagIdsAsync(
            IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
        {
            if( tagIds == null)
            {
                return new List<Tag>();
            }

            if( tagIds.Any(t => t <= 0))
            {
                throw new InvalidOperationException("包含不合法的標籤 ID");
            }

            var distinctTagIds = tagIds.Distinct().ToList();

            if( ! distinctTagIds.Any())
            {
                return new List<Tag>();
            }

            var searchedTagIds = await _dbContext.Tags.Where(t => distinctTagIds.Contains(t.Id)).ToListAsync(cancellationToken);

            if(distinctTagIds.Count != searchedTagIds.Count)
            {
                throw new InvalidOperationException("包含不合法的標籤 ID");
            }

            return searchedTagIds;

        }

        private static string NormalizeDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            // 全形空白變半形
            var value = name.Replace('\u3000', ' ').Trim();
            // 把連續一個以上的空白字元，全部換成一個半形空白
            value = Regex.Replace(value, @"\s+", " ");
            // 如果一個半形空白的左邊是中文、右邊也是中文，就把這個空白移除
            value = Regex.Replace(value, @"(?<=[\u4E00-\u9FFF]) (?=[\u4E00-\u9FFF])", "");

            return value;
        }

        private static string NormalizeKey(string name)
        {
            return NormalizeDisplayName(name).ToLowerInvariant();
        }

        private static TagResponse MapToResponse(Tag tag, int? usageCount = null)
        {
            return new TagResponse
            {
                TagId = tag.Id,
                Name = tag.Name,
                TagCategory = tag.TagCategory,
                UsageCount = usageCount
            };
        }

        private static bool IsValidTagCategory(TagCategory? tagCategory)
        {
            return !tagCategory.HasValue || Enum.IsDefined(typeof(TagCategory), tagCategory.Value);
        }
    }
}
