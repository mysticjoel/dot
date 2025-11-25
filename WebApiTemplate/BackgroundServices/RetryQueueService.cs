using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiTemplate.Configuration;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.BackgroundServices
{
    /// <summary>
    /// Background service that monitors and processes expired payment attempts
    /// </summary>
    public class RetryQueueService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RetryQueueService> _logger;
        private readonly int _retryCheckIntervalSeconds;

        public RetryQueueService(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<PaymentSettings> paymentSettings,
            ILogger<RetryQueueService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _retryCheckIntervalSeconds = paymentSettings.Value.RetryCheckIntervalSeconds;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RetryQueueService started. Check interval: {Interval} seconds",
                _retryCheckIntervalSeconds);

            // Wait a bit before starting to allow app initialization
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("RetryQueueService checking for expired payment attempts");

                    // Create a scope to get scoped services (DbContext)
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var paymentService = scope.ServiceProvider
                            .GetRequiredService<IPaymentService>();

                        // Get all expired payment attempts
                        var expiredAttempts = await paymentService.GetExpiredPaymentAttemptsAsync();

                        if (expiredAttempts.Count > 0)
                        {
                            _logger.LogInformation("RetryQueueService found {Count} expired payment attempts to process",
                                expiredAttempts.Count);

                            foreach (var attempt in expiredAttempts)
                            {
                                try
                                {
                                    _logger.LogInformation(
                                        "Processing expired payment attempt {PaymentId} for auction {AuctionId}",
                                        attempt.PaymentId, attempt.AuctionId);

                                    await paymentService.ProcessFailedPaymentAsync(attempt.PaymentId);

                                    _logger.LogInformation(
                                        "Successfully processed expired payment attempt {PaymentId}",
                                        attempt.PaymentId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex,
                                        "Error processing expired payment attempt {PaymentId}",
                                        attempt.PaymentId);
                                    // Continue with other attempts even if one fails
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RetryQueueService while checking expired payment attempts");
                    // Continue running even if an error occurs
                }

                // Wait for the configured interval before next check
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_retryCheckIntervalSeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when stopping
                    _logger.LogInformation("RetryQueueService stopping due to cancellation");
                    break;
                }
            }

            _logger.LogInformation("RetryQueueService stopped");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RetryQueueService is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}

