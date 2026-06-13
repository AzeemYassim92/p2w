using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Infrastructure.Data;

namespace P2W.Cards.Infrastructure.BackgroundJobs;

public sealed class PriceRefreshWorker(IServiceScopeFactory scopeFactory, IOptions<CardOptions> options, ILogger<PriceRefreshWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(Math.Max(1, options.Value.PriceRefreshHours)), stoppingToken);
            await RefreshWatchlistedCards(stoppingToken);
        }
    }

    private async Task RefreshWatchlistedCards(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
        var listings = scope.ServiceProvider.GetRequiredService<IListingService>();
        var prices = scope.ServiceProvider.GetRequiredService<IPriceHistoryService>();
        var alerts = scope.ServiceProvider.GetRequiredService<IPriceAlertService>();
        var cardIds = await db.WatchlistItems.Select(w => w.CardId).Distinct().ToListAsync(ct);

        foreach (var cardId in cardIds)
        {
            try
            {
                await listings.RefreshListingsForCardAsync(cardId, ct);
                await prices.CaptureListingSnapshotForCardAsync(cardId, ct);
                await prices.RefreshReferencePricesForCardAsync(cardId, ct);
                await alerts.CheckAlertsForCardAsync(cardId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed refreshing card {CardId}; continuing.", cardId);
            }
        }
    }
}
