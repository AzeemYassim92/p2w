using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;

namespace P2W.Cards.Worker.Aggregation;

public sealed class MarketAggregationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MarketAggregationOptions> options,
    ILogger<MarketAggregationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Market aggregation worker is disabled. Set MarketAggregation:Enabled=true to run scheduled refreshes.");
            return;
        }

        logger.LogInformation("Market aggregation worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var aggregation = scope.ServiceProvider.GetRequiredService<IMarketAggregationService>();
                var request = new MarketRefreshRequest
                {
                    UseMockData = false,
                    MaxProducts = Math.Clamp(options.Value.MaxProductsPerRun, 1, 500)
                };

                await aggregation.RefreshRecentlyViewedAsync(request, stoppingToken);
                await aggregation.RefreshWatchlistedAsync(request, stoppingToken);
                await aggregation.RefreshTrendingAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Market aggregation worker cycle failed.");
            }

            var hours = Math.Max(1, options.Value.RefreshListingsHours);
            await Task.Delay(TimeSpan.FromHours(hours), stoppingToken);
        }
    }
}
