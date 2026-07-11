using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stycue.Api.Data;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Points;
using Stycue.Api.Entities;
using Stycue.Api.Enums;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Services
{
    public class PointService : IPointService
    {
        private readonly AppDbContext _dbContext;
        private readonly IOptions<PointsOptions> _pointsOptions;
        private readonly IMapper _mapper;

        public PointService(AppDbContext dbContext, IOptions<PointsOptions> pointsOptions, IMapper mapper)
        {
            _dbContext = dbContext;
            _pointsOptions = pointsOptions;
            _mapper = mapper;
        }

        // Interface public methods
        public async Task<ApiResponse<PointWalletResponse>> GetMyWalletAsync(
              int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }


            var wallet = await EnsureWalletAsync(userId, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = MapWalletResponse(wallet);

            return ApiResponse<PointWalletResponse>.SuccessResult(response, "積分錢包查詢成功");
        }

        public async Task<ApiResponse<PointWalletResponse>> AddPointsAsync(
            int userId, int amount, PointTransactionType transactionType, PointReferenceType referenceType,
            int? referenceId, string? description = null, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            if (amount <= 0)
            {
                return ApiResponse<PointWalletResponse>.FailResult("積分數量必須大於 0", "INVALID_POINT_AMOUNT");
            }

            if (!Enum.IsDefined(typeof(PointTransactionType), transactionType))
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的交易類型", "INVALID_TRANSACTION_TYPE");
            }

            if (!Enum.IsDefined(typeof(PointReferenceType), referenceType))
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的交易關聯來源", "INVALID_REFERENCE_TYPE");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return ApiResponse<PointWalletResponse>.FailResult("查無此使用者", "USER_NOT_FOUND");
            }

            var wallet = await EnsureWalletAsync(user.Id, cancellationToken);

            wallet.CurrentPoints += amount;
            wallet.LifetimeEarnedPoints += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new PointTransaction
            {
                UserId = user.Id,
                Amount = amount,
                TransactionType = transactionType,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Description = ResolveDescription(transactionType, description),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PointTransactions.Add(transaction);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<PointWalletResponse>.SuccessResult(MapWalletResponse(wallet), "積分新增成功");
        }

        public async Task<ApiResponse<PointWalletResponse>> SpendPointsAsync(
            int userId, int amount, PointTransactionType transactionType, PointReferenceType referenceType,
            int? referenceId, string? description = null, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            if (amount <= 0)
            {
                return ApiResponse<PointWalletResponse>.FailResult("積分數量必須大於 0", "INVALID_POINT_AMOUNT");
            }

            if (!Enum.IsDefined(typeof(PointTransactionType), transactionType))
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的交易類型", "INVALID_TRANSACTION_TYPE");
            }

            if (!Enum.IsDefined(typeof(PointReferenceType), referenceType))
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的交易關聯來源", "INVALID_REFERENCE_TYPE");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return ApiResponse<PointWalletResponse>.FailResult("查無此使用者", "USER_NOT_FOUND");
            }

            var wallet = await EnsureWalletAsync(user.Id, cancellationToken);

            if (wallet.CurrentPoints < amount)
            {
                return ApiResponse<PointWalletResponse>.FailResult("積分餘額不足", "INSUFFICIENT_POINTS");
            }

            wallet.CurrentPoints -= amount;
            wallet.LifetimeSpentPoints += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new PointTransaction
            {
                UserId = user.Id,
                Amount = -amount,
                TransactionType = transactionType,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Description = ResolveDescription(transactionType, description),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PointTransactions.Add(transaction);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<PointWalletResponse>.SuccessResult(MapWalletResponse(wallet), "積分扣除成功");
        }

        public async Task<ApiResponse<PointWalletResponse>> GrantRegistrationRewardAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<PointWalletResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            var registrationRewardPoints = _pointsOptions.Value.RegistrationRewardPoints;

            if (registrationRewardPoints <= 0)
            {
                return await GetMyWalletAsync(userId, cancellationToken);
            }

            var alreadyGranted = await _dbContext.PointTransactions.AnyAsync(
                t => t.UserId == userId && t.TransactionType == PointTransactionType.RegistrationReward, cancellationToken);

            if (alreadyGranted)
            {
                return await GetMyWalletAsync(userId, cancellationToken);
            }

            return await AddPointsAsync(
                userId, registrationRewardPoints,
                PointTransactionType.RegistrationReward, PointReferenceType.Registration,
                referenceId: null, description: null, cancellationToken);
        }

        public async Task<ApiResponse<DailyPointClaimResponse>> ClaimDailyAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<DailyPointClaimResponse>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return ApiResponse<DailyPointClaimResponse>.FailResult("查無此使用者", "USER_NOT_FOUND");
            }

            var today = GetTWDate();

            var existingClaim = await _dbContext.DailyPointClaims.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClaimDate == today, cancellationToken);

            var wallet = await EnsureWalletAsync(user.Id, cancellationToken);

            if (existingClaim != null)
            {
                var claimedResponse = new DailyPointClaimResponse
                {
                    IsClaimed = true,
                    ClaimDate = existingClaim.ClaimDate,
                    Points = existingClaim.Points,
                    CurrentPoints = wallet.CurrentPoints,
                    CreatedAt = existingClaim.CreatedAt
                };

                return ApiResponse<DailyPointClaimResponse>.SuccessResult(claimedResponse, "今日已領取積分");
            }

            var dailyRewardPoint = _pointsOptions.Value.DailyRewardPoints;

            if (dailyRewardPoint <= 0)
            {
                return ApiResponse<DailyPointClaimResponse>.FailResult(
                    "每日領取積分設定必須大於 0", "INVALID_DAILY_REWARD_POINTS");
            }

            wallet.CurrentPoints += dailyRewardPoint;
            wallet.LifetimeEarnedPoints += dailyRewardPoint;
            wallet.UpdatedAt = DateTime.UtcNow;

            var dailyClaim = new DailyPointClaim
            {
                UserId = user.Id,
                ClaimDate = today,
                Points = dailyRewardPoint,
                CreatedAt = DateTime.UtcNow
            };

            var transaction = new PointTransaction
            {
                UserId = user.Id,
                Amount = dailyRewardPoint,
                TransactionType = PointTransactionType.DailyReward,
                ReferenceType = PointReferenceType.DailyClaim,
                ReferenceId = null,
                Description = ResolveDescription(PointTransactionType.DailyReward, null),
                CreatedAt = dailyClaim.CreatedAt
            };

            _dbContext.DailyPointClaims.Add(dailyClaim);
            _dbContext.PointTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new DailyPointClaimResponse
            {
                IsClaimed = true,
                ClaimDate = dailyClaim.ClaimDate,
                Points = dailyClaim.Points,
                CurrentPoints = wallet.CurrentPoints,
                CreatedAt = dailyClaim.CreatedAt
            };

            return ApiResponse<DailyPointClaimResponse>.SuccessResult(response, "每日積分領取成功");
        }

        public async Task<ApiResponse<PagedResponse<PointTransactionResponse>>> GetTransactionsAsync(
            int userId, PointTransactionQueryRequest query, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return ApiResponse<PagedResponse<PointTransactionResponse>>.FailResult("不合法的使用者 ID", "INVALID_USER_ID");
            }

            if (query == null)
            {
                query = new PointTransactionQueryRequest();
            }

            if (query.TransactionType.HasValue &&
                !Enum.IsDefined(typeof(PointTransactionType), query.TransactionType.Value))
            {
                return ApiResponse<PagedResponse<PointTransactionResponse>>.FailResult(
                    "不合法的交易類型", "INVALID_TRANSACTION_TYPE");
            }

            if(query.ReferenceType.HasValue &&
                !Enum.IsDefined(typeof(PointReferenceType), query.ReferenceType.Value))
            {
                return ApiResponse<PagedResponse<PointTransactionResponse>>.FailResult(
                    "不合法的交易關聯來源", "INVALID_REFERENCE_TYPE");
            }

            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            var transactionsQuery = _dbContext.PointTransactions.AsNoTracking().Where(t => t.UserId == userId);

            if (query.TransactionType.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(x => x.TransactionType == query.TransactionType.Value);
            }
            if (query.ReferenceType.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(
                    x => x.ReferenceType == query.ReferenceType.Value);
            }

            if (query.ReferenceId.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(
                    x => x.ReferenceId == query.ReferenceId.Value);
            }

            if (query.StartAt.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(
                    x => x.CreatedAt >= query.StartAt.Value);
            }

            if (query.EndAt.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(
                    x => x.CreatedAt <= query.EndAt.Value);
            }

            var totalCount = await transactionsQuery.CountAsync(cancellationToken);

            var transactions = await transactionsQuery
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = transactions.Select(MapTransactionResponse).ToList();

            var response = new PagedResponse<PointTransactionResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return ApiResponse<PagedResponse<PointTransactionResponse>>.SuccessResult(response, "積分交易紀錄查詢成功");
        }

        // private helper methods

        // 取得使用者積分錢包
        private async Task<UserPointWallet> EnsureWalletAsync(int userId, CancellationToken cancellationToken)
        {
            var wallet = await _dbContext.UserPointWallets
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (wallet != null)
            {
                return wallet;
            }

            wallet = new UserPointWallet
            {
                UserId = userId,
                CurrentPoints = 0,
                LifetimeEarnedPoints = 0,
                LifetimeSpentPoints = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UserPointWallets.Add(wallet);

            return wallet;
        }

        // map user wallet to response DTO
        private PointWalletResponse MapWalletResponse(UserPointWallet wallet)
        {
            return _mapper.Map<PointWalletResponse>(wallet);
        }

        // map point transaction to response DTO
        private PointTransactionResponse MapTransactionResponse(PointTransaction transaction)
        {
            return _mapper.Map<PointTransactionResponse>(transaction);
        }

        // make sure Point transaction description has value
        private static string ResolveDescription(PointTransactionType transactionType, string? description)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description.Trim();
            }

            return transactionType switch
            {
                PointTransactionType.RegistrationReward => "註冊贈送積分",
                PointTransactionType.DailyReward => "每日領取積分",
                PointTransactionType.CommissionCreate => "建立委託扣除積分",
                PointTransactionType.CommissionBoost => "委託加碼/重新發表委託扣除積分",
                PointTransactionType.CommissionBestCommentReward => "最佳留言獲得積分",
                PointTransactionType.CommissionAutoReward => "最高讚留言獲得積分",
                PointTransactionType.CommissionRefund => "委託積分退還",
                PointTransactionType.CommissionFee => "委託積分手續費",
                _ => "積分異動"
            };
        }

        private static DateOnly GetTWDate()
        {
            var twTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            var twNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, twTimeZone);

            return DateOnly.FromDateTime(twNow);
        }
    }
}
