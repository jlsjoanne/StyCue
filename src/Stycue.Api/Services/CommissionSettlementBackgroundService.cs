using Microsoft.Extensions.Options;
using Stycue.Api.Options;
using Stycue.Api.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Stycue.Api.Services
{
    public sealed class CommissionSettlementBackgroundService :BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<CommissionSettlementOptions> _options;
        private readonly ILogger<CommissionSettlementBackgroundService> _logger;

        public CommissionSettlementBackgroundService(
            IServiceScopeFactory scopeFactory, 
            IOptions<CommissionSettlementOptions> options, 
            ILogger<CommissionSettlementBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = _options.Value;

            if(!options.Enabled)
            {
                _logger.LogInformation("Commission settlement background service is disabled.");

                return;
            }

            if( options.IntervalMinutes <= 0)
            {
                throw new InvalidOperationException("CommissionSettlement:IntervalMinutes 必須大於 0");
            }

            if( options.StartupDelaySeconds < 0)
            {
                throw new InvalidOperationException("CommissionSettlement:StartupDelaySeconds 不可小於 0");
            }

            if( options.BatchSize <= 0)
            {
                throw new InvalidOperationException("CommissionSettlement:BatchSize 必須大於 0");
            }

            try
            {
                if( options.StartupDelaySeconds > 0)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(options.StartupDelaySeconds), stoppingToken);
                }

                using var timer = new PeriodicTimer(
                    TimeSpan.FromMinutes(options.IntervalMinutes));

                do
                {
                    try
                    {
                        await using var scope = _scopeFactory.CreateAsyncScope();

                        var settlementService = scope.ServiceProvider
                            .GetRequiredService<ICommissionSettlementService>();

                        await settlementService.RunOnceAsync(
                            options.BatchSize, stoppingToken);
                    }
                    catch(OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex,
                            "Commission settlement background run failed.");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch(OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Commission settlement background service stopped.");
            }
        }
    }
}
