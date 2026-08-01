using Stycue.Api.Services.Models;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Data;
using Stycue.Api.Options;
using Microsoft.Extensions.Options;

namespace Stycue.Api.Services
{
    public class CommissionSettlementService : ICommissionSettlementService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPointService _pointService;
        private readonly INotificationService _notificationService;
        private readonly IOptions<PointsOptions> _pointOptions;
        private readonly ILogger<CommissionSettlementService> _logger;

        private const int BestCommentSelectionGraceHours = 24;

        public CommissionSettlementService(
            AppDbContext dbContext, IPointService pointService, 
            INotificationService notificationService, IOptions<PointsOptions> pointOptions,
            ILogger<CommissionSettlementService> logger)
        {
            _dbContext = dbContext;
            _pointService = pointService;
            _notificationService = notificationService;
            _pointOptions = pointOptions;
            _logger = logger;
        }

        public async Task<CommissionSettlementRunResult> RunOnceAsync(
            // place holder
            int batchSize, CancellationToken cancellationToken)
        {
            return new CommissionSettlementRunResult();
        }
    }
}
