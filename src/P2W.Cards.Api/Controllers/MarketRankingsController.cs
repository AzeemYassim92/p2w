using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;
using P2W.Cards.Infrastructure.Services;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/rankings")]
public sealed class MarketRankingsController(CardsDbContext db) : ControllerBase
{
    [HttpGet("trending")]
    public Task<IReadOnlyList<SetMarketDashboardProductDto>> Trending([FromQuery] string? gameSlug, [FromQuery] int take = 25, CancellationToken ct = default)
        => Rank(gameSlug, take, "Trending", r => Math.Abs(r.PriceChangePercent ?? 0m) + (r.OpportunityScore ?? 0m), ct);

    [HttpGet("high-volume")]
    public Task<IReadOnlyList<SetMarketDashboardProductDto>> HighVolume([FromQuery] string? gameSlug, [FromQuery] int take = 25, CancellationToken ct = default)
        => Rank(gameSlug, take, "HighVolume", r => r.ListingCount + r.SoldCount, ct);

    [HttpGet("movers")]
    public Task<IReadOnlyList<SetMarketDashboardProductDto>> Movers([FromQuery] string? gameSlug, [FromQuery] string window = "7d", [FromQuery] int take = 25, CancellationToken ct = default)
        => Rank(gameSlug, take, "Movers", r => Math.Abs(r.PriceChangePercent ?? 0m), ct);

    [HttpGet("opportunities")]
    public Task<IReadOnlyList<SetMarketDashboardProductDto>> Opportunities([FromQuery] string? gameSlug, [FromQuery] int take = 25, CancellationToken ct = default)
        => Rank(gameSlug, take, "Opportunities", r => r.OpportunityScore ?? 0m, ct);

    [HttpGet("deals")]
    public Task<IReadOnlyList<SetMarketDashboardProductDto>> Deals([FromQuery] string? gameSlug, [FromQuery] int take = 25, CancellationToken ct = default)
        => Rank(gameSlug, take, "Deals", r => r.OpportunityScore ?? 0m, ct);

    private async Task<IReadOnlyList<SetMarketDashboardProductDto>> Rank(string? gameSlug, int take, string mode, Func<SetMarketDashboardProductDto, decimal> score, CancellationToken ct)
    {
        var query = db.CatalogProducts
            .Include(p => p.Game)
            .Include(p => p.CardSet)
            .Include(p => p.ProductCategory)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(gameSlug))
        {
            query = query.Where(p => p.Game != null && p.Game.Slug == gameSlug);
        }

        var maxRows = Math.Clamp(take, 1, 100);
        var products = await query
            .OrderByDescending(p => p.IsTrending)
            .ThenByDescending(p => p.CardSet != null && p.CardSet.ReleaseDate.HasValue ? p.CardSet.ReleaseDate : DateTime.MinValue)
            .ThenBy(p => p.CardSet != null ? p.CardSet.Name : "")
            .ThenBy(p => p.CardNumber)
            .ThenBy(p => p.Name)
            .Take(Math.Max(maxRows * 4, 100))
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToArray();
        var realEvidenceIds = (await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => productIds.Contains(r.CatalogProductId)))
                .Select(r => r.CatalogProductId)
                .Distinct()
                .ToListAsync(ct))
            .Concat(await MarketDataProvenance.RealListings(db.CatalogMarketplaceListings.Where(l => productIds.Contains(l.CatalogProductId)))
                .Select(l => l.CatalogProductId)
                .Distinct()
                .ToListAsync(ct))
            .Concat(await MarketDataProvenance.RealSales(db.CatalogMarketplaceSales.Where(s => productIds.Contains(s.CatalogProductId)))
                .Select(s => s.CatalogProductId)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var metricList = await db.CatalogMarketMetrics
            .Where(m => productIds.Contains(m.CatalogProductId))
            .OrderByDescending(m => m.ComputedAtUtc)
            .ToListAsync(ct);
        var metrics = metricList
            .GroupBy(m => m.CatalogProductId)
            .ToDictionary(g => g.Key, g => g.First());

        var snapshotList = await MarketDataProvenance.CustomerSnapshots(db.CatalogMarketPriceSnapshots.Where(s => productIds.Contains(s.CatalogProductId)))
            .OrderByDescending(s => s.CapturedAtUtc)
            .ToListAsync(ct);
        var snapshots = snapshotList
            .GroupBy(s => s.CatalogProductId)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = new List<SetMarketDashboardProductDto>();
        foreach (var product in products)
        {
            metrics.TryGetValue(product.Id, out var metric);
            snapshots.TryGetValue(product.Id, out var snapshot);
            var hasRealEvidence = realEvidenceIds.Contains(product.Id);
            var row = ToRow(product, metric, snapshot, hasRealEvidence, mode);
            row.RankingScore = score(row);
            rows.Add(row);
        }

        var marketRows = rows.Where(r => r.HasMarketData).OrderByDescending(r => r.RankingScore).ThenBy(r => r.ProductName).Take(maxRows).ToArray();
        if (marketRows.Length > 0)
        {
            return marketRows;
        }

        return rows
            .OrderByDescending(r => products.First(p => p.Id == r.CatalogProductId).IsTrending)
            .ThenBy(r => r.GameName)
            .ThenBy(r => r.SetName)
            .ThenBy(r => r.CardNumber)
            .ThenBy(r => r.ProductName)
            .Take(maxRows)
            .ToArray();
    }

    private static SetMarketDashboardProductDto ToRow(CatalogProduct product, CatalogMarketMetric? metric, CatalogMarketPriceSnapshot? snapshot, bool hasRealEvidence, string mode)
    {
        var hiddenDemoData = !hasRealEvidence && (metric != null || snapshot != null);
        if (!hasRealEvidence)
        {
            metric = null;
            snapshot = null;
        }

        var hasData = hasRealEvidence && (metric != null || snapshot != null);
        var current = metric?.CurrentMarketPrice ?? snapshot?.MedianSoldPrice ?? snapshot?.ReferenceMarketPrice ?? snapshot?.MedianListingPrice ?? snapshot?.LowestListingPrice;
        var listings = metric?.ListingCount ?? snapshot?.ListingCount ?? 0;
        var sold = metric?.SoldCount ?? snapshot?.SoldCount ?? 0;
        var confidence = metric?.ConfidenceScore ?? (snapshot == null ? 0 : ScoreConfidence(snapshot));
        var row = new SetMarketDashboardProductDto
        {
            CatalogProductId = product.Id,
            ProductName = product.Name,
            GameName = product.Game?.Name ?? "",
            SetName = product.CardSet?.Name,
            CardNumber = product.CardNumber,
            CategoryName = product.ProductCategory?.Name,
            ImageUrl = product.ImageUrl,
            CurrentMarketPrice = current,
            PriceChangePercent = metric?.PriceChangePercent,
            ListingCount = listings,
            SoldCount = sold,
            OpportunityScore = metric?.OpportunityScore,
            ConfidenceLabel = hasData ? ConfidenceLabel(confidence) : "Needs Refresh",
            HasMarketData = hasData,
            IsDemoData = hiddenDemoData
        };

        (row.SignalLabel, row.SignalDetail) = Signal(row, mode);
        return row;
    }

    private static decimal ScoreConfidence(CatalogMarketPriceSnapshot snapshot)
    {
        var score = 0m;
        if (snapshot.ListingCount >= 20) score += 45;
        else if (snapshot.ListingCount >= 5) score += 30;
        else if (snapshot.ListingCount > 0) score += 15;
        if (snapshot.SoldCount > 0) score += 20;
        if (snapshot.ReferenceMarketPrice.HasValue) score += 25;
        score += 10;
        return Math.Clamp(score, 0, 100);
    }

    private static string ConfidenceLabel(decimal? score)
        => score switch { >= 80 => "High", >= 55 => "Medium", > 0 => "Low", _ => "Insufficient" };

    private static (string Label, string Detail) Signal(SetMarketDashboardProductDto row, string mode)
    {
        if (!row.HasMarketData)
        {
            if (row.IsDemoData)
            {
                return ("Demo data hidden", "Only mock/demo rows exist for this product. Run a real provider refresh before ranking it.");
            }

            return ("No market snapshot", "Run a market refresh to capture listings, sold comps, and price history.");
        }

        return mode switch
        {
            "HighVolume" => ("Volume", $"{row.ListingCount} active listings / {row.SoldCount} sold comps"),
            "Movers" => ("Price move", row.PriceChangePercent.HasValue ? $"{row.PriceChangePercent.Value:+0.0;-0.0;0.0}% over the current window" : "Needs a previous snapshot"),
            "Opportunities" => ("Opportunity", row.OpportunityScore.HasValue ? $"{row.OpportunityScore.Value:0}/100 opportunity score" : "Needs margin and spread inputs"),
            "Deals" => ("Deal readiness", row.OpportunityScore.HasValue ? $"{row.OpportunityScore.Value:0}/100 deal score candidate" : "Needs active listings under market"),
            _ => ("Trend signal", row.PriceChangePercent.HasValue ? $"{row.PriceChangePercent.Value:+0.0;-0.0;0.0}% price movement" : $"{row.ListingCount + row.SoldCount} market signals")
        };
    }
}
