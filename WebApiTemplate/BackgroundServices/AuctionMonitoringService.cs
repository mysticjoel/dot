using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiTemplate.Configuration;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.BackgroundServices
{
    /// <summary>
    /// Background service that monitors and finalizes expired auctions
    /// </summary>
    public class AuctionMonitoringService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AuctionMonitoringService> _logger;
        private readonly int _monitoringIntervalSeconds;

        public AuctionMonitoringService(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<AuctionSettings> auctionSettings,
            ILogger<AuctionMonitoringService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _monitoringIntervalSeconds = auctionSettings.Value.MonitoringIntervalSeconds;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuctionMonitoringService started. Monitoring interval: {Interval} seconds",
                _monitoringIntervalSeconds);

            // Wait a bit before starting to allow app initialization
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("AuctionMonitoringService checking for expired auctions");

                    // Create a scope to get scoped services (DbContext)
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var auctionExtensionService = scope.ServiceProvider
                            .GetRequiredService<IAuctionExtensionService>();

                        // Finalize expired auctions
                        var finalizedCount = await auctionExtensionService.FinalizeExpiredAuctionsAsync();

                        if (finalizedCount > 0)
                        {
                            _logger.LogInformation("AuctionMonitoringService finalized {Count} expired auctions",
                                finalizedCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AuctionMonitoringService while checking expired auctions");
                    // Continue running even if an error occurs
                }

                // Wait for the configured interval before next check
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_monitoringIntervalSeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when stopping
                    _logger.LogInformation("AuctionMonitoringService stopping due to cancellation");
                    break;
                }
            }

            _logger.LogInformation("AuctionMonitoringService stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AuctionMonitoringService is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}

