using AutoMapper;
using Microsoft.Extensions.Options;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Commissions;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Enums;
using Stycue.Api.DTOs.Images;
using Stycue.Api.DTOs.Tags;
using Stycue.Api.DTOs.Points;

namespace Stycue.Api.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITagService _tagService;
        private readonly IPointService _pointService;
        private readonly IImageService _imageService;
        private readonly IImageResponseBuilder _imageResponseBuilder;
        private readonly IUserSummaryResponseBuilder _userSummaryResponseBuilder;
        private readonly IMapper _mapper;
        private readonly IOptions<PointsOptions> _pointOptions;
        private readonly ILogger<CommissionService> _logger;

        public CommissionService(
            AppDbContext dbContext, ITagService tagService, IPointService pointService, 
            IImageService imageService, IImageResponseBuilder imageResponseBuilder, 
            IUserSummaryResponseBuilder userSummaryResponseBuilder,
            IMapper mapper, IOptions<PointsOptions> pointoptions, ILogger<CommissionService> logger)
        {
            _dbContext = dbContext;
            _tagService = tagService;
            _pointService = pointService;
            _imageService = imageService;
            _imageResponseBuilder = imageResponseBuilder;
            _userSummaryResponseBuilder = userSummaryResponseBuilder;
            _mapper = mapper;
            _pointOptions = pointoptions;
            _logger = logger;
        }

        // Interface Public Methods
        // 取得委託文詳細內容
        public async Task<ApiResponse<CommissionDetailResponse>> GetCommissionAsync(
              int? userId,
              int commissionId,
              CancellationToken cancellationToken = default)
        {
            if( commissionId <= 0)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("不合法的委託文 ID", "INVALID_COMMISSION_ID");
            }

            var commission = await FindCommissionForDetailAsync(commissionId, cancellationToken);

            if( commission == null)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var isClosed = commission.Status == CommissionStatus.Closed || commission.ClosedAt != null;
            var isOwner = OwnershipGuard.IsOwner(commission.UserId, userId);

            if(isClosed && !isOwner)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult(
                    "找不到指定的委託文", "COMMISSION_NOT_FOUND");
            }

            var response = BuildCommissionDetailResponse(commission, userId);

            return ApiResponse<CommissionDetailResponse>.SuccessResult(response);
        }

        // 建立委託
        public async Task<ApiResponse<CommissionDetailResponse>> CreateAsync(
            int userId,
            CreateCommissionRequest request,
            CancellationToken cancellationToken = default)
        {
            if(request == null)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("建立委託資料不可為空", "INVALID_REQUEST");
            }

            if(userId <= 0)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            var title = request.Title?.Trim() ?? string.Empty;
            var content = request.Content?.Trim() ?? string.Empty;
            var budget = request.Budget?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("委託標題不可為空", "COMMISSION_TITLE_REQUIRED");
            }

            if(title.Length > 100)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("委託標題不可超過 100 個字", "COMMISSION_TITLE_TOO_LONG");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("委託內容不可為空", "COMMISSION_CONTENT_REQUIRED");
            }

            if(content.Length > 4000)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("委託內容不可超過 4000 個字", "COMMISSION_CONTENT_TOO_LONG");
            }

            if(request.Height < 1 || request.Height > 300)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("身高需介於 1 到 300 公分", "INVALID_HEIGHT");
            }

            if(request.Weight < 1 || request.Weight > 500)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("體重需介於 1 到 500 公斤", "INVALID_WEIGHT");
            }

            if(request.Age < 1 || request.Age > 120)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("年齡需介於 1 到 120 歲", "INVALID_AGE");
            }

            if(budget.Length > 100)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("預算描述不可超過 100 個字", "COMMISSION_BUDGET_TOO_LONG");
            }

            if( request.Points < _pointOptions.Value.MinCommissionPoints)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult(
                    $"建立委託至少需要 {_pointOptions.Value.MinCommissionPoints} 積分",
                    "COMMISSION_POINTS_TOO_LOW");
            }

            var imagesResult = await _imageService.ValidateBindableImagesAsync(
                userId, request.ImageIds, ImagePurpose.Commission, cancellationToken);

            if(!imagesResult.Success)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult(imagesResult.Message, imagesResult.ErrorCode);
            }

            var commissionImages = imagesResult.Data ?? [];

            var commissionTags = await _tagService.ValidateTagIdsAsync(
                request.TagIds, cancellationToken);

            if(!commissionTags.Success)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult(commissionTags.Message, commissionTags.ErrorCode);
            }

            int commissionId = 0;
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // get current time
                var now = DateTime.UtcNow;

                // create commission object
                var commission = new Commission
                {
                    UserId = userId,
                    Title = title,
                    Content = content,
                    Height = request.Height,
                    Weight = request.Weight,
                    Age = request.Age,
                    Budget = budget,
                    Points = request.Points,
                    Status = CommissionStatus.Open,
                    RepostCount = 0,
                    CreatedAt = now,
                    ExpiredAt = now.AddDays(_pointOptions.Value.DefaultCommissionExpireDays)
                };

                _dbContext.Commissions.Add(commission);

                // save to db to get commission id
                await _dbContext.SaveChangesAsync(cancellationToken);

                // check wallet and make sure point is enough for commission
                // if success => spend points
                var spendResult = await _pointService.SpendPointsAsync(
                    userId, commission.Points, PointTransactionType.CommissionCreate,
                    PointReferenceType.Commission, commission.Id, $"建立委託文：{commission.Title}", cancellationToken);

                if(!spendResult.Success)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return ApiResponse<CommissionDetailResponse>.FailResult(spendResult.Message, spendResult.ErrorCode);
                }

                // bind images to commission
                BindCommissionImages(commission.Id, commissionImages);

                // bind tags to commission
                BindCommissionTags(commission, commissionTags.Tags);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                commissionId = commission.Id;

            }
            catch(Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch(Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx,
                        "Rollback create commission transaction failed. UserId: {UserId}",
                        userId);
                }
                
                _logger.LogError(ex, "Create commission failed. UserId: {UserId}", userId);

                return ApiResponse<CommissionDetailResponse>.FailResult("建立委託文失敗，請稍後再試", "COMMISSION_CREATE_FAILED");
            }

            var detail = await FindCommissionForDetailAsync(commissionId, cancellationToken);

            if (detail == null)
            {
                _logger.LogError(
                    "Commission was created but detail query returned null. CommissionId: {CommissionId}, UserId: {UserId}",
                    commissionId, userId);

                return ApiResponse<CommissionDetailResponse>.FailResult("委託文建立成功，但讀取詳情失敗", "COMMISSION_DETAIL_NOT_FOUND_AFTER_CREATE");
            }

            var response = BuildCommissionDetailResponse(detail, userId);

            return ApiResponse<CommissionDetailResponse>.SuccessResult(response, "委託文建立成功");
        }

        // 提前關閉委託
        public async Task<ApiResponse<CloseCommissionResponse>> CloseAsync(
            int userId,
            int commissionId,
            CancellationToken cancellationToken = default)
        {
            // check userId
            if (userId <= 0)
            {
                return ApiResponse<CloseCommissionResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            // check commission Id
            if (commissionId <= 0)
            {
                return ApiResponse<CloseCommissionResponse>.FailResult("不合法的委託文 ID", "INVALID_COMMISSION_ID");
            }

            var refundPercent = _pointOptions.Value.RefundPercent;

            if (refundPercent <= 0 || refundPercent > 100)
            {
                return ApiResponse<CloseCommissionResponse>.FailResult(
                    "委託退點比例設定錯誤",
                    "INVALID_REFUND_PERCENT");
            }

            if( _pointOptions.Value.EarlyCloseLimitHours <= 0)
            {
                return ApiResponse<CloseCommissionResponse>.FailResult(
                    "提前關閉時間限制設定錯誤", "INVALID_EARLY_CLOSE_LIMIT_HOURS");
            }

            // start transaction 
            // 確認委託可關閉 → 關閉委託→ 退還積分→ 建立 PointTransaction

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // get commission
                var commission = await _dbContext.Commissions.FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);

                if( commission == null)
                {
                    return ApiResponse<CloseCommissionResponse>.FailResult(
                        "找不到指定的委託文", "COMMISSION_NOT_FOUND");
                }

                // validate commission ownership
                var ownerError = OwnershipGuard.EnsureOwner<CloseCommissionResponse>(commission.UserId, userId,
                    "只有委託建立者可以關閉委託", "COMMISSION_NOT_OWNER");

                if(ownerError != null)
                {
                    return ownerError;
                }

                // check Commission status
                if (commission.Status == CommissionStatus.Closed ||
                    commission.Status == CommissionStatus.Rewarded ||
                    commission.Status == CommissionStatus.NoAward ||
                    commission.ClosedAt != null ||
                    commission.AwardedCommentId != null ||
                    commission.AwardedAt != null ||
                    commission.RewardSettledAt != null)
                {
                    return ApiResponse<CloseCommissionResponse>.FailResult(
                        "目前委託狀態無法關閉", "COMMISSION_CANNOT_CLOSE");
                }

                var now = DateTime.UtcNow;
                var closeDeadline = commission.CreatedAt.AddHours(_pointOptions.Value.EarlyCloseLimitHours);

                if (now > closeDeadline)
                {
                    return ApiResponse<CloseCommissionResponse>.FailResult(
                        $"委託建立超過 {_pointOptions.Value.EarlyCloseLimitHours} 小時後不可提前關閉",
                        "COMMISSION_CLOSE_WINDOW_EXPIRED");
                }

                var alreadyRefunded = await _dbContext.PointTransactions.AnyAsync(t =>
                    t.TransactionType == PointTransactionType.CommissionRefund &&
                    t.ReferenceType == PointReferenceType.Commission &&
                    t.ReferenceId == commission.Id,
                    cancellationToken);

                if (alreadyRefunded)
                {
                    return ApiResponse<CloseCommissionResponse>.FailResult(
                        "此委託已完成退點，無法重複關閉",
                        "COMMISSION_ALREADY_REFUNDED");
                }

                var refundPoints = (int)Math.Ceiling(commission.Points * refundPercent / 100m);
                var feePoints = commission.Points - refundPoints;

                commission.Status = CommissionStatus.Closed;
                commission.ClosedAt = now;
                commission.UpdatedAt = now;
                commission.RewardSettledAt = now;

                var refundResult = await _pointService.AddPointsAsync(
                    userId, refundPoints, PointTransactionType.CommissionRefund, PointReferenceType.Commission,
                    commission.Id, $"提前關閉委託退還積分：{commission.Title}", cancellationToken);

                if (!refundResult.Success)
                {
                    return ApiResponse<CloseCommissionResponse>.FailResult(
                        refundResult.Message, refundResult.ErrorCode);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new CloseCommissionResponse
                {
                    CommissionId = commission.Id,
                    Status = commission.Status,
                    ClosedAt = commission.ClosedAt.Value,
                    RefundedPoints = refundPoints,
                    FeePoints = feePoints
                };

                return ApiResponse<CloseCommissionResponse>.SuccessResult(response, "委託已關閉，積分已退還");
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx,
                        "Rollback close commission transaction failed. CommissionId: {CommissionId}, UserId: {UserId}",
                        commissionId,
                        userId);
                }

                _logger.LogError(ex,
                    "Close commission failed. CommissionId: {CommissionId}, UserId: {UserId}",
                    commissionId, userId);

                return ApiResponse<CloseCommissionResponse>.FailResult("關閉委託失敗，請稍後再試",
                    "COMMISSION_CLOSE_FAILED");
            }
        }


        // 到期後補充內容並重新開啟委託
        public async Task<ApiResponse<CommissionDetailResponse>> RepostAsync(
            int userId,
            int commissionId,
            RepostCommissionRequest request,
            CancellationToken cancellationToken = default)
        {
            // check userId
            if( userId <= 0)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            // check commission Id
            if (commissionId <= 0)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("不合法的委託文 ID", "INVALID_COMMISSION_ID");
            }

            // check request
            if ( request == null)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("重新開啟委託資料不可為空",
                    "INVALID_REQUEST");
            }

            // check supplement content
            var supplementContent = request.SupplementContent?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(supplementContent))
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("補充內容不可為空",
                    "COMMISSION_REPOST_CONTENT_REQUIRED");
            }

            if(supplementContent.Length > 4000)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("補充內容不可超過 4000 個字",
                    "COMMISSION_REPOST_CONTENT_TOO_LONG");
            }

            if(request.AdditionalPoints < 0)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult("追加積分不可小於 0",
                    "INVALID_ADDITIONAL_POINTS");
            }

            // check Images
            var imageResult = await _imageService.ValidateBindableImagesAsync(userId,
                request.ImageIds, ImagePurpose.Commission, cancellationToken);

            if(!imageResult.Success)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult(imageResult.Message, imageResult.ErrorCode);
            }

            var repostImages = imageResult.Data ?? [];

            // check tags
            var repostTags = await _tagService.ValidateTagIdsAsync(request.TagIds, cancellationToken);

            if(!repostTags.Success)
            {
                return ApiResponse<CommissionDetailResponse>.FailResult(repostTags.Message, repostTags.ErrorCode);
            }


            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // get commission
                var commission = await FindCommissionForUpdateAsync(commissionId, cancellationToken);

                if( commission == null)
                {
                    return ApiResponse<CommissionDetailResponse>.FailResult("找不到指定的委託文",
                        "COMMISSION_NOT_FOUND");
                }

                // check ownership
                var ownerError = OwnershipGuard.EnsureOwner<CommissionDetailResponse>(commission.UserId, userId,
                    "只有委託建立者可以重新開啟委託", "COMMISSION_NOT_OWNER");

                if( ownerError != null)
                {
                    return ownerError;
                }

                var now = DateTime.UtcNow;
                var isExpired = IsExpired(commission, now);

                var hasActiveComments = await _dbContext.Comments.AnyAsync(c =>
                    c.CommissionId == commission.Id &&
                    c.DeletedAt == null, cancellationToken);
                var hasExistingRepost = commission.Reposts.Any();

                if( !CanRepost(commission, isOwner: true, isExpired, hasActiveComments, hasExistingRepost))
                {
                    return ApiResponse<CommissionDetailResponse>.FailResult(
                        "目前委託狀態無法重新開啟", "COMMISSION_CANNOT_REPOST");
                }

                // if setting additional points
                if( request.AdditionalPoints > 0)
                {
                    var spendResult = await _pointService.SpendPointsAsync(userId, request.AdditionalPoints,
                        PointTransactionType.CommissionBoost, PointReferenceType.Commission,
                        commission.Id, $"重新開啟委託加碼積分：{commission.Title}", cancellationToken);

                    if(!spendResult.Success)
                    {
                        return ApiResponse<CommissionDetailResponse>.FailResult(spendResult.Message, spendResult.ErrorCode);
                    }
                }

                // create commission repost object
                var repost = new CommissionRepost
                {
                    CommissionId = commission.Id,
                    UserId = userId,
                    SupplementContent = supplementContent,
                    AdditionalPoints = request.AdditionalPoints,
                    CreatedAt = now
                };

                _dbContext.CommissionReposts.Add(repost);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // bind Images
                BindCommissionImages(commission.Id, repostImages, repost.Id);
                BindCommissionTags(commission, repostTags.Tags);

                // update commission
                commission.Points += request.AdditionalPoints;
                commission.RepostCount += 1;
                commission.Status = CommissionStatus.Open;
                commission.ExpiredAt = now.AddDays(_pointOptions.Value.DefaultCommissionExtendDays);
                commission.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

            }
            catch(DbUpdateException dbex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch(Exception rollbackex)
                {
                    _logger.LogError(rollbackex,
                        "Rollback repost commission transaction failed after db update exception. CommissionId: {CommissionId}, UserId: {UserId}",
                        commissionId, userId);
                }

                _logger.LogWarning(dbex,
                    "Repost commission db update failed. Possible duplicate repost. CommissionId: {CommissionId}, UserId: {UserId}",
                    commissionId, userId);

                return ApiResponse<CommissionDetailResponse>.FailResult("此委託已重新開啟過，無法再次重新開啟",
                    "COMMISSION_REPOST_LIMIT_REACHED");
            }
            catch(Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch(Exception rollbackex)
                {
                    _logger.LogError(rollbackex,
                        "Rollback repost commission transaction failed. CommissionId: {CommissionId}, UserId: {UserId}",
                        commissionId, userId);
                }

                _logger.LogError(ex,
                    "Repost commission failed. CommissionId: {CommissionId}, UserId: {UserId}",
                    commissionId, userId);

                return ApiResponse<CommissionDetailResponse>.FailResult(
                    "重新開啟委託失敗，請稍後再試", "COMMISSION_REPOST_FAILED");
            }


            var detail = await FindCommissionForDetailAsync(commissionId, cancellationToken);
            if( detail == null)
            {
                _logger.LogError("Commission was reposted but detail query returned null. CommissionId: {CommissionId}, UserId: {UserId}",
                    commissionId, userId);

                return ApiResponse<CommissionDetailResponse>.FailResult("委託已重新開啟，但讀取詳情失敗",
                    "COMMISSION_DETAIL_NOT_FOUND_AFTER_REPOST");
            }

            var response = BuildCommissionDetailResponse(detail, userId);

            return ApiResponse<CommissionDetailResponse>.SuccessResult(response,
                "委託已重新開啟");
        }

        // 加碼委託文積分並延長到期時間
        public async Task<ApiResponse<BoostCommissionResponse>> BoostAsync(
            int userId,
            int commissionId,
            BoostCommissionRequest request,
            CancellationToken cancellationToken = default)
        {
            // check userId
            if (userId <= 0)
            {
                return ApiResponse<BoostCommissionResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            // check commission Id
            if (commissionId <= 0)
            {
                return ApiResponse<BoostCommissionResponse>.FailResult("不合法的委託文 ID", "INVALID_COMMISSION_ID");
            }

            if( request == null)
            {
                return ApiResponse<BoostCommissionResponse>.FailResult(
                    "加碼委託資料不可為空", "INVALID_REQUEST");
            }

            if(request.AdditionalPoints < _pointOptions.Value.MinCommissionBoostPoints)
            {
                return ApiResponse<BoostCommissionResponse>.FailResult(
                    $"加碼積分至少需要 {_pointOptions.Value.MinCommissionBoostPoints} 積分",
                    "COMMISSION_BOOST_POINTS_TOO_LOW");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // get commission
                var commission = await _dbContext.Commissions
                    .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);

                // check if get commission
                if( commission == null)
                {
                    return ApiResponse<BoostCommissionResponse>.FailResult("找不到指定的委託文",
                        "COMMISSION_NOT_FOUND");
                }

                // check if user is commission owner
                var ownerError = OwnershipGuard.EnsureOwner<BoostCommissionResponse>(
                    commission.UserId, userId, "只有委託建立者可以加碼委託", "COMMISSION_NOT_OWNER");

                if(ownerError != null)
                {
                    return ownerError;
                }

                // check if commission can be boosted
                if( !CanBoost(commission, isOwner: true))
                {
                    return ApiResponse<BoostCommissionResponse>.FailResult(
                        "目前委託狀態無法加碼", "COMMISSION_CANNOT_BOOST");
                }

                // user spend additional point for boosting
                var spendResult = await _pointService.SpendPointsAsync(userId, request.AdditionalPoints,
                    PointTransactionType.CommissionBoost, PointReferenceType.Commission,
                    commission.Id, $"委託加碼積分：{commission.Title}", cancellationToken);

                if(!spendResult.Success)
                {
                    return ApiResponse<BoostCommissionResponse>.FailResult(spendResult.Message, spendResult.ErrorCode);
                }

                var now = DateTime.UtcNow;

                // update commission status
                commission.Points += request.AdditionalPoints;
                commission.Status = CommissionStatus.Open;
                commission.ExpiredAt = now.AddDays(_pointOptions.Value.DefaultCommissionExtendDays);
                commission.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new BoostCommissionResponse
                {
                    CommissionId = commission.Id,
                    Status = commission.Status,
                    AddedPoints = request.AdditionalPoints,
                    TotalPoints = commission.Points,
                    ExpiredAt = commission.ExpiredAt,
                    Wallet = spendResult.Data ?? new PointWalletResponse()
                };

                return ApiResponse<BoostCommissionResponse>.SuccessResult(response, "委託加碼成功");
            }
            catch(Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch(Exception rollEx)
                {
                    _logger.LogError(rollEx,
                        "Rollback boost commission transaction failed. CommissionId: {CommissionId}, UserId: {UserId}",
                        commissionId, userId);
                }

                _logger.LogError(ex,
                    "Boost commission failed. CommissionId: {CommissionId}, UserId: {UserId}",
                    commissionId, userId);

                return ApiResponse<BoostCommissionResponse>.FailResult("委託加碼失敗，請稍後再試", "COMMISSION_BOOST_FAILED");
            }
        }

        // 委託者手動選擇最佳留言並發放積分
        public async Task<ApiResponse<CommissionRewardResponse>> SelectBestCommentAsync(
            int userId,
            int commissionId,
            SelectBestCommentRequest request,
            CancellationToken cancellationToken = default)
        {
            if( userId <= 0)
            {
                return ApiResponse<CommissionRewardResponse>.FailResult(
                    "不合法的使用者 ID", "INVALID_USER_ID");
            }

            if(commissionId <= 0)
            {
                return ApiResponse<CommissionRewardResponse>.FailResult(
                    "不合法的委託文 ID", "INVALID_COMMISSION_ID");
            }

            if(request == null)
            {
                return ApiResponse<CommissionRewardResponse>.FailResult(
                    "選擇最佳留言資料不可為空", "INVALID_REQUEST");
            }

            if(request.CommentId <= 0)
            {
                return ApiResponse<CommissionRewardResponse>.FailResult(
                    "不合法的留言 ID", "INVALID_COMMENT_ID");
            }

            var feePercent = _pointOptions.Value.FeePercent;

            if( feePercent < 0 || feePercent > 100)
            {
                return ApiResponse<CommissionRewardResponse>.FailResult(
                    "委託手續費比例設定錯誤", "INVALID_FEE_PERCENT");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // get commission
                var commission = await FindCommissionForUpdateAsync(commissionId, cancellationToken);

                // check commission
                if( commission == null)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "找不到指定的委託文", "COMMISSION_NOT_FOUND");
                }

                // check owner
                var ownerError = OwnershipGuard.EnsureOwner<CommissionRewardResponse>(
                    commission.UserId, userId, "只有委託建立者可以選擇最佳留言", "COMMISSION_NOT_OWNER");

                if( ownerError != null)
                {
                    return ownerError;
                }

                // 已結算防線
                if( commission.AwardedCommentId != null ||
                    commission.AwardedAt != null ||
                    commission.RewardSettledAt != null ||
                    commission.Status == CommissionStatus.Rewarded)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "此委託已完成獎勵結算，無法重複發放積分", "COMMISSION_REWARD_ALREADY_SETTLED");
                }

                //  驗證是否為不可操作狀態
                //  Closed
                if( commission.Status == CommissionStatus.Closed || commission.ClosedAt != null)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "已關閉的委託無法選擇最佳留言", "COMMISSION_CLOSED");
                }

                // No Award
                if(commission.Status == CommissionStatus.NoAward)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "此委託已流標，無法選擇最佳留言", "COMMISSION_NO_AWARD");
                }

                // final check
                if( !CanSelectBestComment(commission, isOwner: true))
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "目前委託狀態無法選擇最佳留言", "COMMISSION_CANNOT_SELECT_BEST_COMMENT");
                }

                // filter out comments that can be selected as best comment
                var awardedComment = FindActiveCommissionComment(commission, request.CommentId);

                if( awardedComment == null)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "找不到可被選為最佳留言的留言", "COMMENT_NOT_SELECTABLE");
                }

                var alreadyPaid = await _dbContext.PointTransactions.AnyAsync(
                    t => t.TransactionType == PointTransactionType.CommissionBestCommentReward &&
                    t.ReferenceType == PointReferenceType.Commission &&
                    t.ReferenceId == commission.Id, cancellationToken);

                if (alreadyPaid)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "此委託已發放過最佳留言積分", "COMMISSION_REWARD_ALREADY_PAID");
                }

                var feePoints = (int)Math.Ceiling(commission.Points * feePercent / 100m);
                var rewardPoints = commission.Points - feePoints;

                if( rewardPoints <= 0)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        "可發放獎勵積分必須大於 0", "INVALID_REWARD_POINTS");
                }

                // give points to best comment
                var rewardResult = await _pointService.AddPointsAsync(
                    awardedComment.UserId, rewardPoints, PointTransactionType.CommissionBestCommentReward,
                    PointReferenceType.Commission, commission.Id,
                    $"最佳留言獲得委託積分：{commission.Title}", cancellationToken);

                if(!rewardResult.Success)
                {
                    return ApiResponse<CommissionRewardResponse>.FailResult(
                        rewardResult.Message, rewardResult.ErrorCode);
                }

                // update commission status
                var now = DateTime.UtcNow;

                commission.Status = CommissionStatus.Rewarded;
                commission.AwardedCommentId = awardedComment.Id;
                commission.AwardedAt = now;
                commission.RewardSettledAt = now;
                commission.UpdatedAt = now;

                // save to db
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = BuildCommissionRewardResponse(commission, awardedComment,
                    rewardPoints, now, rewardResult.Data ?? new PointWalletResponse());

                return ApiResponse<CommissionRewardResponse>.SuccessResult(response,
                    "最佳留言已選擇，積分已發放");
            }
            catch(Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch(Exception rollEx)
                {
                    _logger.LogError(rollEx,
                        "Rollback select best comment transaction failed. CommissionId: {CommissionId}, UserId: {UserId}",
                        commissionId, userId);
                }

                _logger.LogError(ex,
                    "Select best comment failed. CommissionId: {CommissionId}, UserId: {UserId}, CommentId: {CommentId}",
                    commissionId, userId, request?.CommentId);

                return ApiResponse<CommissionRewardResponse>.FailResult(
                    "選擇最佳留言失敗，請稍後再試", "COMMISSION_SELECT_BEST_COMMENT_FAILED");
            }
        }

        

        // private helper methods

        // 委託文是否到期
        private static bool IsExpired(Commission commission, DateTime now)
        {
            return commission.ExpiredAt <= now;
        }

        // 委託文是否可以Repost
        private static bool CanRepost(Commission commission, bool isOwner, bool isExpired,
            bool hasActiveComments, bool hasExistingReposts)
        {
            return isOwner && isExpired && !hasActiveComments && !hasExistingReposts &&
                commission.Status != CommissionStatus.Closed &&
                commission.Status != CommissionStatus.Rewarded &&
                commission.Status != CommissionStatus.NoAward &&
                commission.ClosedAt == null && commission.AwardedCommentId == null &&
                commission.AwardedAt == null &&
                commission.RepostCount == 0 &&
                commission.RewardSettledAt == null;
        }

        // 委託文是否可以Boost

        private static bool CanBoost(Commission commission, bool isOwner)
        {
            return isOwner &&
                commission.Status != CommissionStatus.Rewarded &&
                commission.Status != CommissionStatus.Closed &&
                commission.Status != CommissionStatus.NoAward &&
                commission.ClosedAt == null &&
                commission.RewardSettledAt == null &&
                commission.AwardedAt == null &&
                commission.AwardedCommentId == null;
        }

        // 委託文是否可以選最佳留言

        private static bool CanSelectBestComment(Commission commission, bool isOwner)
        {
            return isOwner &&
                commission.AwardedCommentId == null &&
                commission.RewardSettledAt == null &&
                commission.Status != CommissionStatus.Closed &&
                commission.Status != CommissionStatus.Rewarded &&
                commission.Status != CommissionStatus.NoAward &&
                commission.Comments.Any(comment => comment.DeletedAt == null && comment.ParentCommentId == null);
        }

        // 查詢委託文詳情
        private async Task<Commission?> FindCommissionForDetailAsync(
            int commissionId, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions.AsNoTracking().AsSplitQuery()
                .Include(c => c.User).ThenInclude(u => u.AvatarImage)
                .Include(c => c.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.CommissionTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.Reposts).ThenInclude(r => r.Images).ThenInclude(i => i.FashionMetadata)
                .Include(c => c.Comments)
                .Include(c => c.CommissionLikes)
                .Include(c => c.CommissionFavorites)
                .FirstOrDefaultAsync(c => c.Id == commissionId, cancellationToken);
        }

        // 找到需要更新狀態的委託文
        // 用於Close / Repost / Boost / BestComment / SettleReward
        private async Task<Commission?> FindCommissionForUpdateAsync(
            int commissionId, CancellationToken cancellationToken)
        {
            return await _dbContext.Commissions
                .Include(c => c.Comments).ThenInclude(comment => comment.CommentLikes)
                .Include(c => c.Reposts)
                .Include(c => c.CommissionTags)
                .FirstOrDefaultAsync(c => c.Id == commissionId,cancellationToken);
        }

        // 將可被選為最佳留言的基本條件集中
        // 只找根留言、排除 reply、排除已刪除
        // 篩選出被委託者選為最佳的留言
        private static Comment? FindActiveCommissionComment(
            Commission commission, int commentId)
        {
            return commission.Comments.FirstOrDefault(comment =>
                comment.Id == commentId &&
                comment.CommissionId == commission.Id &&
                comment.ParentCommentId == null &&
                comment.DeletedAt == null);
        }

        // 綁定標籤
        private static void BindCommissionTags(
            Commission commission, IEnumerable<Tag> tags)
        {
            var existingTagIds = commission.CommissionTags.Select(x => x.TagId).ToHashSet();

            foreach(var tag in tags)
            {
                if (existingTagIds.Contains(tag.Id))
                {
                    continue;
                }

                commission.CommissionTags.Add(new CommissionTag
                {
                    Commission = commission,
                    TagId = tag.Id
                });
            }
        }

        // 綁定圖片
        private static void BindCommissionImages(
            int commissionId, IEnumerable<ImageAsset> images, int? commissionRepostId = null)
        {
            foreach(var image in images)
            {
                image.CommissionId = commissionId;
                image.CommissionRepostId = commissionRepostId;
            }
        }
        

        // 建立Commission Response圖片
        private IReadOnlyList<ImageResponse> BuildOriginalCommissionImages(Commission commission)
        {
            return _imageResponseBuilder.BuildList(
                commission.Images.Where(i => i.CommissionRepostId == null)
                .OrderBy(i => i.CreatedAt));
        }

        // 建立Commission Response 委託重發內容
        private IReadOnlyList<CommissionRepostResponse> BuildCommissionReposts(Commission commission)
        {
            return commission.Reposts
                .OrderBy(r => r.CreatedAt)
                .Select(repost =>
                {
                    var response = _mapper.Map<CommissionRepostResponse>(repost);

                    response.Images = _imageResponseBuilder.BuildList(repost.Images.OrderBy(image => image.CreatedAt));

                    return response;
                })
                .ToList();
        }

        // 建立回傳委託文詳細內容
        private CommissionDetailResponse BuildCommissionDetailResponse(
            Commission commission, int? currentUserId)
        {
            var now = DateTime.UtcNow;
            var isOwner = OwnershipGuard.IsOwner(commission.UserId, currentUserId);
            var isExpired = IsExpired(commission, now);

            var response = _mapper.Map<CommissionDetailResponse>(commission);
            var hasActiveComments = commission.Comments.Any(c => c.DeletedAt == null);
            var hasExistingReposts = commission.Reposts.Any();

            response.Author = _userSummaryResponseBuilder.Build(commission.User);

            response.IsOwner = isOwner;
            response.IsExpired = isExpired;
            response.CanBoost = CanBoost(commission, isOwner);
            response.CanRepost = CanRepost(commission, isOwner, isExpired, hasActiveComments, hasExistingReposts);
            response.CanSelectBestComment = CanSelectBestComment(commission, isOwner);

            response.CommentCount = commission.Comments.Count(c => c.DeletedAt == null);
            response.LikeCount = commission.CommissionLikes.Count;
            response.FavoriteCount = commission.CommissionFavorites.Count;
            response.IsLiked = currentUserId.HasValue ? commission.CommissionLikes.Any(like => like.UserId == currentUserId.Value) : null;
            response.IsFavorited = currentUserId.HasValue ? commission.CommissionFavorites.Any(f => f.UserId == currentUserId.Value) : null;

            response.Images = BuildOriginalCommissionImages(commission);
            response.Tags = commission.CommissionTags
                .OrderBy(ct => ct.Tag.Name)
                .Select(ct => _mapper.Map<TagResponse>(ct.Tag))
                .ToList();

            response.Reposts = BuildCommissionReposts(commission);

            return response;
        }

        // 建立委託文發積分response
        private static CommissionRewardResponse BuildCommissionRewardResponse(
            Commission commission, Comment awardedComment, int rewardPoints,
            DateTime awardedAt, PointWalletResponse receiverWallet)
        {
            return new CommissionRewardResponse
            {
                CommissionId = commission.Id,
                Status = commission.Status,
                AwardedCommentId = awardedComment.Id,
                RewardReceiverUserId = awardedComment.UserId,
                RewardPoints = rewardPoints,
                AwardedAt = awardedAt,
                ReceiverWallet = receiverWallet
            };
        }
    }
}
