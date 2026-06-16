using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;
using P2W.Cards.Application.Options;
using P2W.Cards.Domain.Entities;
using P2W.Cards.Infrastructure.Data;

namespace P2W.Cards.Infrastructure.Services;

public sealed class MockMarketDataProvider : IMarketplaceReferencePriceProvider, IMarketplaceActiveListingProvider, IMarketplaceSoldCompsProvider
{
    public string SourceName => "MockMarket";
    public bool IsEnabled => true;

    public Task<IReadOnlyList<ExternalReferencePriceDto>> GetReferencePricesAsync(CatalogProduct product, IReadOnlyList<ExternalProductMapping> mappings, CancellationToken ct)
    {
        var price = DemoBasePrice(product);
        IReadOnlyList<ExternalReferencePriceDto> rows = new[]
        {
            new ExternalReferencePriceDto
            {
                SourceName = SourceName,
                MarketplaceSourceId = CardsDbContext.MockMarketMarketplaceSourceId,
                Condition = "NearMint",
                MarketPrice = price,
                LowPrice = price * 0.86m,
                MidPrice = price,
                HighPrice = price * 1.28m,
                UngradedPrice = price,
                SalesVolume = (StableSeed(product) % 70) + 5,
                Currency = "USD",
                CapturedAtUtc = DateTime.UtcNow,
                ExternalUrl = $"https://example.com/mock-market/{product.Slug}",
                RawSourceJson = "{\"source\":\"demo\"}"
            }
        };
        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<ExternalMarketplaceListingDto>> GetCurrentListingsAsync(CatalogProduct product, MarketplaceSearchContext context, CancellationToken ct)
    {
        var price = DemoBasePrice(product);
        var seed = StableSeed(product);
        var now = DateTime.UtcNow;
        var rows = Enumerable.Range(0, 8).Select(index =>
        {
            var discount = 0.78m + ((seed + index * 7) % 38) / 100m;
            var listingPrice = decimal.Round(price * discount, 2);
            var shipping = index % 3 == 0 ? 4.99m : 0m;
            return new ExternalMarketplaceListingDto
            {
                SourceName = SourceName,
                ExternalListingId = $"mock-{product.Id:N}-{index}",
                ExternalSku = product.Slug,
                Title = $"{product.Name} {product.CardNumber} {product.CardSet?.Name} Near Mint",
                Price = listingPrice,
                ShippingPrice = shipping,
                EffectivePrice = listingPrice + shipping,
                Currency = "USD",
                Condition = "NearMint",
                RawCondition = "NM",
                Quantity = 1,
                SellerName = $"DemoSeller{index + 1}",
                SellerFeedbackScore = 1000 + index * 127,
                SellerFeedbackPercentage = 98.5m + index % 2,
                SellerLocation = "US",
                ListingUrl = $"https://example.com/mock-listing/{product.Id:N}/{index}",
                ImageUrl = product.ImageUrl,
                IsAuction = index % 5 == 0,
                AuctionEndsUtc = index % 5 == 0 ? now.AddDays(2) : null,
                ListedAtUtc = now.AddDays(-index - 1),
                CapturedAtUtc = now,
                LastSeenUtc = now,
                MatchConfidence = index == 7 ? 0.82m : 0.94m,
                MatchStatus = index == 7 ? "NeedsReview" : "Matched",
                IsExcludedFromMarketValue = index == 7,
                ExclusionReason = index == 7 ? "Demo low-confidence comparable" : null,
                RawSourceJson = "{\"source\":\"demo\"}"
            };
        }).ToArray();
        return Task.FromResult<IReadOnlyList<ExternalMarketplaceListingDto>>(rows);
    }

    public Task<IReadOnlyList<ExternalMarketplaceSaleDto>> GetRecentSalesAsync(CatalogProduct product, MarketplaceSearchContext context, DateTime sinceUtc, CancellationToken ct)
    {
        var price = DemoBasePrice(product);
        var seed = StableSeed(product);
        var now = DateTime.UtcNow;
        var rows = Enumerable.Range(0, 16).Select(index =>
        {
            var factor = 0.84m + ((seed + index * 11) % 32) / 100m;
            var soldPrice = decimal.Round(price * factor, 2);
            return new ExternalMarketplaceSaleDto
            {
                SourceName = SourceName,
                ExternalSaleId = $"mock-sale-{product.Id:N}-{index}",
                ExternalSku = product.Slug,
                Title = $"{product.Name} sold comparable {index + 1}",
                SoldPrice = soldPrice,
                ShippingPrice = 4.50m,
                EffectiveSoldPrice = soldPrice + 4.50m,
                Currency = "USD",
                Condition = "NearMint",
                Quantity = 1,
                SellerName = $"DemoSeller{index + 1}",
                SoldAtUtc = now.AddDays(-(index * 4 + 1)),
                CapturedAtUtc = now,
                SaleUrl = $"https://example.com/mock-sale/{product.Id:N}/{index}",
                ImageUrl = product.ImageUrl,
                MatchConfidence = 0.93m,
                MatchStatus = "Matched",
                RawSourceJson = "{\"source\":\"demo\"}"
            };
        }).Where(s => s.SoldAtUtc >= sinceUtc).ToArray();
        return Task.FromResult<IReadOnlyList<ExternalMarketplaceSaleDto>>(rows);
    }

    internal static decimal DemoBasePrice(CatalogProduct product)
    {
        if (product.IsSealed)
        {
            return product.ProductType.Contains("Box", StringComparison.OrdinalIgnoreCase) ? 119.99m : 49.99m;
        }

        var seed = StableSeed(product);
        return decimal.Round(((seed % 18000) / 100m) + 4.99m, 2);
    }

    internal static int StableSeed(CatalogProduct product)
        => Math.Abs(HashCode.Combine(product.Name.ToLowerInvariant(), product.CardNumber ?? "", product.CardSet?.Name ?? ""));
}

public sealed class MarketAggregationService(
    CardsDbContext db,
    IEnumerable<IMarketplaceReferencePriceProvider> referenceProviders,
    IEnumerable<IMarketplaceActiveListingProvider> listingProviders,
    IEnumerable<IMarketplaceSoldCompsProvider> soldProviders,
    IMarketMetricsService metrics,
    MarketDiagnosticTrail diagnostics) : IMarketAggregationService
{
    private static readonly TimeSpan FreshMarketDataWindow = TimeSpan.FromHours(6);

    public async Task<MarketAggregationResultDto> RefreshProductMarketDataAsync(Guid catalogProductId, MarketRefreshRequest request, CancellationToken ct)
    {
        var product = await LoadProductAsync(catalogProductId, ct) ?? throw new KeyNotFoundException("Catalog product not found.");
        var run = StartRun("MarketAggregation", "ProductRefresh", $"Product {catalogProductId}");
        db.CatalogProviderIngestionRuns.Add(run);
        await db.SaveChangesAsync(ct);

        var result = new MarketAggregationResultDto { RunId = run.Id, Status = "Started", ProductsQueued = 1 };
        try
        {
            var mappings = await db.ExternalProductMappings.Where(m => m.CatalogProductId == catalogProductId).ToListAsync(ct);
            var context = BuildContext(product, request);
            diagnostics.Info("market.refresh.start", "Starting product market refresh.", new
            {
                product.Id,
                product.Name,
                Game = product.Game?.Slug,
                Set = product.CardSet?.Name,
                product.CardNumber,
                request.UseMockData,
                request.Condition,
                request.Currency,
                MappingCount = mappings.Count
            });

            if (!request.Force && await HasFreshMarketEvidenceAsync(product.Id, request.UseMockData, ct))
            {
                result.ProductsSkipped = 1;
                result.Status = "Skipped";
                result.Notes = $"Fresh market evidence exists from the last {FreshMarketDataWindow.TotalHours:0} hours. Use force refresh to query providers again.";
                diagnostics.Info("market.refresh.skipped", "Skipping provider refresh because fresh market evidence already exists.", new
                {
                    product.Id,
                    product.Name,
                    FreshnessHours = FreshMarketDataWindow.TotalHours
                });
                result.DiagnosticEvents = diagnostics.Events;
                FinishRun(run, result.Status, result);
                await db.SaveChangesAsync(ct);
                return result;
            }

            foreach (var provider in referenceProviders)
            {
                if (!provider.IsEnabled)
                {
                    diagnostics.Debug("provider.reference.skipped", "Reference provider is disabled.", new { provider.SourceName });
                    continue;
                }
                if (!SourceAllowed(provider.SourceName, request))
                {
                    diagnostics.Debug("provider.reference.skipped", "Reference provider excluded by request source filter or mock setting.", new { provider.SourceName, request.UseMockData });
                    continue;
                }

                diagnostics.Info("provider.reference.start", "Requesting reference prices.", new { provider.SourceName });
                var prices = await provider.GetReferencePricesAsync(product, mappings, ct);
                diagnostics.Info("provider.reference.complete", "Reference provider returned rows.", new { provider.SourceName, Count = prices.Count });
                if (prices.Count == 0)
                {
                    diagnostics.Warning("provider.reference.empty", "Reference provider returned no usable prices.", new { provider.SourceName });
                }
                foreach (var price in prices)
                {
                    db.CatalogPriceReferenceSnapshots.Add(new CatalogPriceReferenceSnapshot
                    {
                        Id = Guid.NewGuid(),
                        CatalogProductId = product.Id,
                        ProductVariantId = price.ProductVariantId,
                        SourceName = price.SourceName,
                        MarketPrice = price.MarketPrice,
                        LowPrice = price.LowPrice,
                        MidPrice = price.MidPrice,
                        HighPrice = price.HighPrice,
                        UngradedPrice = price.UngradedPrice,
                        Grade7Price = price.Grade7Price,
                        Grade8Price = price.Grade8Price,
                        Grade9Price = price.Grade9Price,
                        Grade10Price = price.Grade10Price,
                        BuylistPrice = price.BuylistPrice,
                        RetailPrice = price.RetailPrice,
                        Currency = price.Currency,
                        RawSourceJson = price.RawSourceJson,
                        CapturedAtUtc = price.CapturedAtUtc == default ? DateTime.UtcNow : price.CapturedAtUtc
                    });
                    result.ReferencePricesCreated++;
                }
            }

            foreach (var provider in listingProviders)
            {
                if (!provider.IsEnabled)
                {
                    diagnostics.Debug("provider.listings.skipped", "Listing provider is disabled.", new { provider.SourceName });
                    continue;
                }
                if (!SourceAllowed(provider.SourceName, request))
                {
                    diagnostics.Debug("provider.listings.skipped", "Listing provider excluded by request source filter or mock setting.", new { provider.SourceName, request.UseMockData });
                    continue;
                }

                diagnostics.Info("provider.listings.start", "Requesting active listings.", new { provider.SourceName });
                var listings = await provider.GetCurrentListingsAsync(product, context, ct);
                diagnostics.Info("provider.listings.complete", "Listing provider returned rows.", new { provider.SourceName, Count = listings.Count });
                if (listings.Count == 0)
                {
                    diagnostics.Warning("provider.listings.empty", "Listing provider returned no usable listings.", new { provider.SourceName });
                }
                foreach (var listing in listings)
                {
                    await UpsertListingAsync(product.Id, listing, ct);
                    result.ListingsCreated++;
                }
            }

            foreach (var provider in soldProviders)
            {
                if (!provider.IsEnabled)
                {
                    diagnostics.Debug("provider.sales.skipped", "Sold-comps provider is disabled.", new { provider.SourceName });
                    continue;
                }
                if (!SourceAllowed(provider.SourceName, request))
                {
                    diagnostics.Debug("provider.sales.skipped", "Sold-comps provider excluded by request source filter or mock setting.", new { provider.SourceName, request.UseMockData });
                    continue;
                }

                diagnostics.Info("provider.sales.start", "Requesting recent sold comps.", new { provider.SourceName });
                var sales = await provider.GetRecentSalesAsync(product, context, DateTime.UtcNow.AddDays(-90), ct);
                diagnostics.Info("provider.sales.complete", "Sold-comps provider returned rows.", new { provider.SourceName, Count = sales.Count });
                if (sales.Count == 0)
                {
                    diagnostics.Warning("provider.sales.empty", "Sold-comps provider returned no usable sales.", new { provider.SourceName });
                }
                foreach (var sale in sales)
                {
                    await UpsertSaleAsync(product.Id, sale, ct);
                }
            }

            await db.SaveChangesAsync(ct);
            var snapshotCreated = await CreateSnapshotAsync(product.Id, request.Condition, request.Currency, request.UseMockData, ct);
            diagnostics.Info("market.snapshot.complete", "Snapshot computation completed.", new { SnapshotCreated = snapshotCreated });
            if (snapshotCreated)
            {
                result.SnapshotsCreated++;
                await db.SaveChangesAsync(ct);
                await metrics.ComputeMetricsForProductAsync(product.Id, request.Condition, request.Currency, ct);
                result.MetricsComputed++;
                diagnostics.Info("market.metrics.complete", "Market metrics computed.", new { result.MetricsComputed });
            }
            if (result.ListingsCreated + result.ListingsUpdated + result.ReferencePricesCreated == 0)
            {
                result.Notes = "No enabled real provider returned a usable listing, sold comp, or reference price for this product. Check provider mapping/search match and provider price payload.";
            }
            result.ProductsProcessed = 1;
            result.Status = "Completed";
            result.DiagnosticEvents = diagnostics.Events;
            FinishRun(run, result.Status, result);
            await db.SaveChangesAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.Errors = 1;
            result.Notes = ex.Message;
            diagnostics.Error("market.refresh.failed", "Market refresh failed.", ex, new { catalogProductId });
            result.DiagnosticEvents = diagnostics.Events;
            FinishRun(run, result.Status, result);
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    public async Task<MarketAggregationResultDto> RefreshSetMarketDataAsync(Guid cardSetId, MarketRefreshRequest request, CancellationToken ct)
    {
        var productIds = await db.CatalogProducts.Where(p => p.CardSetId == cardSetId && p.IsActive).OrderBy(p => p.Name).Take(Math.Clamp(request.MaxProducts, 1, 500)).Select(p => p.Id).ToListAsync(ct);
        return await RefreshManyAsync(productIds, request, "SetRefresh", ct);
    }

    public async Task<MarketAggregationResultDto> RefreshRecentlyViewedAsync(MarketRefreshRequest request, CancellationToken ct)
    {
        var productIds = await db.ProductMarketViewEvents.OrderByDescending(e => e.CreatedUtc).Select(e => e.CatalogProductId).Distinct().Take(Math.Clamp(request.MaxProducts, 1, 500)).ToListAsync(ct);
        return await RefreshManyAsync(productIds, request, "RecentlyViewedRefresh", ct);
    }

    public async Task<MarketAggregationResultDto> RefreshWatchlistedAsync(MarketRefreshRequest request, CancellationToken ct)
    {
        var productIds = await db.CatalogWatchlistItems.OrderByDescending(w => w.CreatedUtc).Select(w => w.CatalogProductId).Distinct().Take(Math.Clamp(request.MaxProducts, 1, 500)).ToListAsync(ct);
        return await RefreshManyAsync(productIds, request, "WatchlistedRefresh", ct);
    }

    public async Task<MarketAggregationResultDto> RefreshTrendingAsync(MarketRefreshRequest request, CancellationToken ct)
    {
        var productIds = await db.CatalogProducts.Where(p => p.IsTrending && p.IsActive).OrderBy(p => p.Name).Take(Math.Clamp(request.MaxProducts, 1, 500)).Select(p => p.Id).ToListAsync(ct);
        return await RefreshManyAsync(productIds, request, "TrendingRefresh", ct);
    }

    public async Task<IReadOnlyList<MarketAggregationResultDto>> GetRunsAsync(int take, CancellationToken ct)
        => await db.CatalogProviderIngestionRuns.OrderByDescending(r => r.StartedUtc).Take(Math.Clamp(take, 1, 100)).Select(r => ToResult(r)).ToListAsync(ct);

    public async Task<MarketAggregationResultDto?> GetRunAsync(Guid runId, CancellationToken ct)
        => await db.CatalogProviderIngestionRuns.Where(r => r.Id == runId).Select(r => ToResult(r)).FirstOrDefaultAsync(ct);

    private async Task<MarketAggregationResultDto> RefreshManyAsync(IReadOnlyList<Guid> productIds, MarketRefreshRequest request, string workload, CancellationToken ct)
    {
        var result = new MarketAggregationResultDto { Status = "Completed", ProductsQueued = productIds.Count };
        foreach (var productId in productIds)
        {
            try
            {
                var child = await RefreshProductMarketDataAsync(productId, request, ct);
                result.ProductsProcessed += child.ProductsProcessed;
                result.ProductsSkipped += child.ProductsSkipped;
                result.ListingsCreated += child.ListingsCreated;
                result.ListingsUpdated += child.ListingsUpdated;
                result.ReferencePricesCreated += child.ReferencePricesCreated;
                result.SnapshotsCreated += child.SnapshotsCreated;
                result.MetricsComputed += child.MetricsComputed;
                result.Errors += child.Errors;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                result.Errors++;
            }
        }
        if (result.Errors > 0)
        {
            result.Status = result.ProductsProcessed + result.ProductsSkipped > 0 ? "CompletedWithErrors" : "Failed";
        }
        result.Notes = workload;
        return result;
    }

    private async Task<bool> HasFreshMarketEvidenceAsync(Guid productId, bool includeDemoData, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Subtract(FreshMarketDataWindow);

        var snapshots = db.CatalogMarketPriceSnapshots.Where(s => s.CatalogProductId == productId && s.CapturedAtUtc >= cutoff);
        if (!includeDemoData)
        {
            snapshots = MarketDataProvenance.CustomerSnapshots(snapshots);
        }
        if (await snapshots.AnyAsync(ct))
        {
            return true;
        }

        var references = db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == productId && r.CapturedAtUtc >= cutoff);
        if (!includeDemoData)
        {
            references = MarketDataProvenance.RealReferenceSnapshots(references);
        }
        if (await references.AnyAsync(ct))
        {
            return true;
        }

        var listings = db.CatalogMarketplaceListings.Where(l => l.CatalogProductId == productId && l.IsActive && l.LastSeenUtc >= cutoff);
        if (!includeDemoData)
        {
            listings = MarketDataProvenance.RealListings(listings);
        }
        if (await listings.AnyAsync(ct))
        {
            return true;
        }

        var sales = db.CatalogMarketplaceSales.Where(s => s.CatalogProductId == productId && s.CapturedAtUtc >= cutoff);
        if (!includeDemoData)
        {
            sales = MarketDataProvenance.RealSales(sales);
        }
        return await sales.AnyAsync(ct);
    }

    private async Task<CatalogProduct?> LoadProductAsync(Guid productId, CancellationToken ct)
        => await db.CatalogProducts.Include(p => p.Game).Include(p => p.CardSet).Include(p => p.ProductCategory).FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, ct);

    private static MarketplaceSearchContext BuildContext(CatalogProduct product, MarketRefreshRequest request) => new()
    {
        GameName = product.Game?.Name ?? "",
        GameSlug = product.Game?.Slug,
        SetName = product.CardSet?.Name,
        SetCode = product.CardSet?.Code,
        CardNumber = product.CardNumber,
        ProductType = product.ProductType,
        CategorySlug = product.ProductCategory?.Slug ?? "",
        Condition = request.Condition,
        Currency = request.Currency
    };

    private async Task UpsertListingAsync(Guid productId, ExternalMarketplaceListingDto listing, CancellationToken ct)
    {
        var source = await FindSourceAsync(listing.SourceName, ct);
        var entity = await db.CatalogMarketplaceListings.FirstOrDefaultAsync(l => l.MarketplaceSourceId == source.Id && l.ExternalListingId == listing.ExternalListingId, ct);
        if (entity == null)
        {
            entity = new CatalogMarketplaceListing { Id = Guid.NewGuid(), MarketplaceSourceId = source.Id, ExternalListingId = listing.ExternalListingId };
            db.CatalogMarketplaceListings.Add(entity);
        }

        entity.CatalogProductId = productId;
        entity.SourceName = listing.SourceName;
        entity.ExternalSku = listing.ExternalSku;
        entity.Title = listing.Title;
        entity.Price = listing.Price;
        entity.ShippingPrice = listing.ShippingPrice;
        entity.EffectivePrice = listing.EffectivePrice == 0 ? listing.Price + (listing.ShippingPrice ?? 0m) : listing.EffectivePrice;
        entity.Currency = listing.Currency;
        entity.Condition = listing.Condition;
        entity.RawCondition = listing.RawCondition;
        entity.Quantity = listing.Quantity;
        entity.SellerName = listing.SellerName;
        entity.SellerFeedbackScore = listing.SellerFeedbackScore;
        entity.SellerFeedbackPercentage = listing.SellerFeedbackPercentage;
        entity.SellerLocation = listing.SellerLocation;
        entity.ListingUrl = listing.ListingUrl;
        entity.ImageUrl = listing.ImageUrl;
        entity.IsAuction = listing.IsAuction;
        entity.AuctionEndsUtc = listing.AuctionEndsUtc;
        entity.ListedAtUtc = listing.ListedAtUtc;
        entity.CapturedAtUtc = listing.CapturedAtUtc == default ? DateTime.UtcNow : listing.CapturedAtUtc;
        entity.LastSeenUtc = listing.LastSeenUtc == default ? DateTime.UtcNow : listing.LastSeenUtc;
        entity.IsActive = true;
        entity.MatchConfidence = listing.MatchConfidence;
        entity.MatchStatus = listing.MatchStatus;
        entity.IsExcludedFromMarketValue = listing.IsExcludedFromMarketValue;
        entity.ExclusionReason = listing.ExclusionReason;
        entity.RawSourceJson = listing.RawSourceJson;
    }

    private async Task UpsertSaleAsync(Guid productId, ExternalMarketplaceSaleDto sale, CancellationToken ct)
    {
        var source = await FindSourceAsync(sale.SourceName, ct);
        var entity = await db.CatalogMarketplaceSales.FirstOrDefaultAsync(s => s.MarketplaceSourceId == source.Id && s.ExternalSaleId == sale.ExternalSaleId, ct);
        if (entity == null)
        {
            entity = new CatalogMarketplaceSale { Id = Guid.NewGuid(), MarketplaceSourceId = source.Id, ExternalSaleId = sale.ExternalSaleId };
            db.CatalogMarketplaceSales.Add(entity);
        }

        entity.CatalogProductId = productId;
        entity.SourceName = sale.SourceName;
        entity.ExternalListingId = sale.ExternalListingId;
        entity.ExternalSku = sale.ExternalSku;
        entity.Title = sale.Title;
        entity.SoldPrice = sale.SoldPrice;
        entity.ShippingPrice = sale.ShippingPrice;
        entity.EffectiveSoldPrice = sale.EffectiveSoldPrice == 0 ? sale.SoldPrice + (sale.ShippingPrice ?? 0m) : sale.EffectiveSoldPrice;
        entity.Currency = sale.Currency;
        entity.Condition = sale.Condition;
        entity.RawCondition = sale.RawCondition;
        entity.Quantity = sale.Quantity;
        entity.SellerName = sale.SellerName;
        entity.SoldAtUtc = sale.SoldAtUtc;
        entity.CapturedAtUtc = sale.CapturedAtUtc == default ? DateTime.UtcNow : sale.CapturedAtUtc;
        entity.SaleUrl = sale.SaleUrl;
        entity.ImageUrl = sale.ImageUrl;
        entity.MatchConfidence = sale.MatchConfidence;
        entity.MatchStatus = sale.MatchStatus;
        entity.IsExcludedFromMarketValue = sale.IsExcludedFromMarketValue;
        entity.ExclusionReason = sale.ExclusionReason;
        entity.RawSourceJson = sale.RawSourceJson;
    }

    private async Task<bool> CreateSnapshotAsync(Guid productId, string condition, string currency, bool isDemoData, CancellationToken ct)
    {
        var listingsQuery = db.CatalogMarketplaceListings.Where(l => l.CatalogProductId == productId && l.IsActive && !l.IsExcludedFromMarketValue && (l.MatchConfidence ?? 0m) >= 0.90m);
        var salesQuery = db.CatalogMarketplaceSales.Where(s => s.CatalogProductId == productId && !s.IsExcludedFromMarketValue && (s.MatchConfidence ?? 0m) >= 0.90m);
        var refsQuery = db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == productId);

        if (!isDemoData)
        {
            listingsQuery = MarketDataProvenance.RealListings(listingsQuery);
            salesQuery = MarketDataProvenance.RealSales(salesQuery);
            refsQuery = MarketDataProvenance.RealReferenceSnapshots(refsQuery);
        }

        var listings = await listingsQuery.ToListAsync(ct);
        var sales = await salesQuery.ToListAsync(ct);
        var refs = await refsQuery.OrderByDescending(r => r.CapturedAtUtc).Take(8).ToListAsync(ct);
        var referencePrices = refs
            .Select(r => r.MarketPrice ?? r.MidPrice ?? r.UngradedPrice)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToArray();

        if (listings.Count == 0 && sales.Count == 0 && referencePrices.Length == 0)
        {
            return false;
        }

        db.CatalogMarketPriceSnapshots.Add(new CatalogMarketPriceSnapshot
        {
            Id = Guid.NewGuid(),
            CatalogProductId = productId,
            SourceName = isDemoData ? MarketDataProvenance.DemoAggregatedSource : "Aggregated",
            Condition = condition,
            Currency = currency,
            LowestListingPrice = listings.Count == 0 ? null : listings.Min(l => l.EffectivePrice),
            MedianListingPrice = Median(listings.Select(l => l.EffectivePrice)),
            AverageListingPrice = listings.Count == 0 ? null : listings.Average(l => l.EffectivePrice),
            HighestListingPrice = listings.Count == 0 ? null : listings.Max(l => l.EffectivePrice),
            LastSoldPrice = sales.OrderByDescending(s => s.SoldAtUtc).Select(s => (decimal?)s.EffectiveSoldPrice).FirstOrDefault(),
            MedianSoldPrice = Median(sales.Select(s => s.EffectiveSoldPrice)),
            AverageSoldPrice = sales.Count == 0 ? null : sales.Average(s => s.EffectiveSoldPrice),
            LowestSoldPrice = sales.Count == 0 ? null : sales.Min(s => s.EffectiveSoldPrice),
            HighestSoldPrice = sales.Count == 0 ? null : sales.Max(s => s.EffectiveSoldPrice),
            ReferenceMarketPrice = Median(referencePrices),
            ReferenceLowPrice = refs.Where(r => r.LowPrice.HasValue).Select(r => r.LowPrice).FirstOrDefault(),
            ReferenceMidPrice = refs.Where(r => r.MidPrice.HasValue).Select(r => r.MidPrice).FirstOrDefault(),
            ReferenceHighPrice = refs.Where(r => r.HighPrice.HasValue).Select(r => r.HighPrice).FirstOrDefault(),
            ListingCount = listings.Count,
            SoldCount = sales.Count,
            SalesVolume = sales.Sum(s => s.EffectiveSoldPrice),
            CapturedAtUtc = DateTime.UtcNow
        });
        return true;
    }

    private async Task<MarketplaceSource> FindSourceAsync(string sourceName, CancellationToken ct)
        => await db.MarketplaceSources.FirstOrDefaultAsync(s => s.Name == sourceName, ct)
            ?? await db.MarketplaceSources.FirstAsync(s => s.Name == "MockMarket", ct);

    private static bool SourceAllowed(string sourceName, MarketRefreshRequest request)
        => (request.UseMockData || !sourceName.Equals("MockMarket", StringComparison.OrdinalIgnoreCase))
            && (request.SourceNames is null || request.SourceNames.Count == 0 || request.SourceNames.Contains(sourceName, StringComparer.OrdinalIgnoreCase));

    private static CatalogProviderIngestionRun StartRun(string sourceName, string workloadType, string? notes) => new()
    {
        Id = Guid.NewGuid(),
        SourceName = sourceName,
        WorkloadType = workloadType,
        StartedUtc = DateTime.UtcNow,
        Status = "Started",
        Notes = notes
    };

    private static void FinishRun(CatalogProviderIngestionRun run, string status, MarketAggregationResultDto result)
    {
        run.FinishedUtc = DateTime.UtcNow;
        run.Status = status;
        run.RecordsProcessed = result.ProductsProcessed;
        run.RecordsCreated = result.ListingsCreated + result.ReferencePricesCreated + result.SnapshotsCreated + result.MetricsComputed;
        run.RecordsSkipped = result.ProductsSkipped;
        run.ErrorCount = result.Errors;
        run.Notes = result.Notes ?? run.Notes;
    }

    private static MarketAggregationResultDto ToResult(CatalogProviderIngestionRun run) => new()
    {
        RunId = run.Id,
        Status = run.Status,
        ProductsProcessed = run.RecordsProcessed,
        ProductsSkipped = run.RecordsSkipped,
        ListingsCreated = run.RecordsCreated,
        Errors = run.ErrorCount,
        Notes = run.Notes
    };

    internal static decimal? Median(IEnumerable<decimal> values)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        if (ordered.Length == 0) return null;
        var mid = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[mid - 1] + ordered[mid]) / 2m : ordered[mid];
    }
}

public static class MarketDataProvenance
{
    public const string MockMarketSource = "MockMarket";
    public const string DemoAggregatedSource = "DemoAggregated";

    public static IQueryable<CatalogPriceReferenceSnapshot> RealReferenceSnapshots(IQueryable<CatalogPriceReferenceSnapshot> query)
        => query.Where(r => r.SourceName != MockMarketSource
            && (r.RawSourceJson == null
                || (!r.RawSourceJson.Contains("\"source\":\"demo\"")
                    && !r.RawSourceJson.Contains("\"source\": \"demo\"")
                    && !r.RawSourceJson.Contains("demo-pokemontcg-reference")
                    && !r.RawSourceJson.Contains("\"source\":\"mock\"")
                    && !r.RawSourceJson.Contains("\"source\": \"mock\""))));

    public static IQueryable<CatalogMarketplaceListing> RealListings(IQueryable<CatalogMarketplaceListing> query)
        => query.Where(l => l.SourceName != MockMarketSource
            && (l.RawSourceJson == null
                || (!l.RawSourceJson.Contains("\"source\":\"demo\"")
                    && !l.RawSourceJson.Contains("\"source\": \"demo\"")
                    && !l.RawSourceJson.Contains("\"source\":\"mock\"")
                    && !l.RawSourceJson.Contains("\"source\": \"mock\""))));

    public static IQueryable<CatalogMarketplaceSale> RealSales(IQueryable<CatalogMarketplaceSale> query)
        => query.Where(s => s.SourceName != MockMarketSource
            && (s.RawSourceJson == null
                || (!s.RawSourceJson.Contains("\"source\":\"demo\"")
                    && !s.RawSourceJson.Contains("\"source\": \"demo\"")
                    && !s.RawSourceJson.Contains("\"source\":\"mock\"")
                    && !s.RawSourceJson.Contains("\"source\": \"mock\""))));

    public static IQueryable<CatalogMarketPriceSnapshot> CustomerSnapshots(IQueryable<CatalogMarketPriceSnapshot> query)
        => query.Where(s => s.SourceName != DemoAggregatedSource);

    public static decimal? ReferenceValue(CatalogPriceReferenceSnapshot reference)
        => reference.MarketPrice ?? reference.MidPrice ?? reference.UngradedPrice ?? reference.LowPrice ?? reference.HighPrice;
}

public sealed class MarketMetricsService(CardsDbContext db, IOptions<MarketFeesOptions> fees) : IMarketMetricsService
{
    public async Task ComputeMetricsForProductAsync(Guid catalogProductId, string condition, string currency, CancellationToken ct)
    {
        var latest = await db.CatalogMarketPriceSnapshots.Where(s => s.CatalogProductId == catalogProductId).OrderByDescending(s => s.CapturedAtUtc).FirstOrDefaultAsync(ct);
        if (latest == null) return;

        var previous = await db.CatalogMarketPriceSnapshots.Where(s => s.CatalogProductId == catalogProductId && s.Id != latest.Id).OrderByDescending(s => s.CapturedAtUtc).FirstOrDefaultAsync(ct);
        var current = latest.MedianSoldPrice ?? latest.ReferenceMarketPrice ?? latest.MedianListingPrice ?? latest.LowestListingPrice;
        var prior = previous?.MedianSoldPrice ?? previous?.ReferenceMarketPrice ?? previous?.MedianListingPrice ?? previous?.LowestListingPrice;
        var change = current.HasValue && prior.HasValue ? current - prior : null;
        var changePct = change.HasValue && prior is > 0 ? change / prior.Value * 100m : null;
        var feePercent = fees.Value.DefaultMarketplaceFeePercent + fees.Value.DefaultPaymentFeePercent;
        decimal? estimatedFees = current.HasValue ? current.Value * (feePercent / 100m) + fees.Value.DefaultPaymentFixedFee : null;
        decimal? netMargin = current.HasValue && estimatedFees.HasValue ? current.Value - estimatedFees.Value - fees.Value.DefaultShippingCost : null;
        var confidence = ScoreConfidence(latest);

        db.CatalogMarketMetrics.Add(new CatalogMarketMetric
        {
            Id = Guid.NewGuid(),
            CatalogProductId = catalogProductId,
            Condition = condition,
            Currency = currency,
            WindowName = "30d",
            WindowStartUtc = DateTime.UtcNow.AddDays(-30),
            WindowEndUtc = DateTime.UtcNow,
            CurrentMarketPrice = current,
            PreviousMarketPrice = prior,
            PriceChangeAmount = change,
            PriceChangePercent = changePct,
            LowPrice = latest.LowestSoldPrice ?? latest.LowestListingPrice ?? latest.ReferenceLowPrice,
            HighPrice = latest.HighestSoldPrice ?? latest.HighestListingPrice ?? latest.ReferenceHighPrice,
            ListingCount = latest.ListingCount,
            SoldCount = latest.SoldCount,
            SalesVolume = latest.SalesVolume,
            TotalSoldValue = latest.SalesVolume,
            AverageSoldValue = latest.AverageSoldPrice,
            VolumeScore = Math.Min(100, (latest.ListingCount + latest.SoldCount) * 5),
            TrendScore = changePct.HasValue ? Math.Clamp(50 + changePct.Value, 0, 100) : 50,
            VolatilityScore = latest.HighestListingPrice.HasValue && latest.LowestListingPrice.HasValue && current is > 0 ? Math.Clamp((latest.HighestListingPrice.Value - latest.LowestListingPrice.Value) / current.Value * 100m, 0, 100) : 25,
            LiquidityScore = Math.Min(100, latest.ListingCount * 8 + latest.SoldCount * 12),
            SpreadScore = latest.HighestListingPrice.HasValue && latest.LowestListingPrice.HasValue && latest.LowestListingPrice > 0 ? Math.Clamp((latest.HighestListingPrice.Value - latest.LowestListingPrice.Value) / latest.LowestListingPrice.Value * 100m, 0, 100) : 0,
            DealScore = 55,
            OpportunityScore = Math.Clamp(confidence * 0.35m + Math.Min(100, latest.ListingCount * 8) * 0.30m + (changePct ?? 0m) * 0.35m, 0, 100),
            ConfidenceScore = confidence,
            EstimatedFeesPercent = feePercent,
            EstimatedShippingCost = fees.Value.DefaultShippingCost,
            EstimatedGrossMargin = current,
            EstimatedNetMargin = netMargin,
            EstimatedRoiPercent = null,
            ComputedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogMarketMetricDto>> GetMetricsAsync(Guid catalogProductId, CancellationToken ct)
        => await db.CatalogMarketMetrics.Where(m => m.CatalogProductId == catalogProductId).OrderByDescending(m => m.ComputedAtUtc).Select(m => new CatalogMarketMetricDto
        {
            CatalogMarketMetricId = m.Id,
            CatalogProductId = m.CatalogProductId,
            Condition = m.Condition,
            Currency = m.Currency,
            WindowName = m.WindowName,
            CurrentMarketPrice = m.CurrentMarketPrice,
            PriceChangePercent = m.PriceChangePercent,
            ListingCount = m.ListingCount,
            SoldCount = m.SoldCount,
            VolumeScore = m.VolumeScore,
            TrendScore = m.TrendScore,
            VolatilityScore = m.VolatilityScore,
            LiquidityScore = m.LiquidityScore,
            SpreadScore = m.SpreadScore,
            DealScore = m.DealScore,
            OpportunityScore = m.OpportunityScore,
            ConfidenceScore = m.ConfidenceScore,
            ComputedAtUtc = m.ComputedAtUtc
        }).ToListAsync(ct);

    internal static decimal ScoreConfidence(CatalogMarketPriceSnapshot snapshot)
    {
        var score = 0m;
        if (snapshot.ListingCount >= 20) score += 45;
        else if (snapshot.ListingCount >= 5) score += 30;
        else if (snapshot.ListingCount > 0) score += 15;
        if (snapshot.SoldCount >= 20) score += 30;
        else if (snapshot.SoldCount >= 5) score += 24;
        else if (snapshot.SoldCount > 0) score += 16;
        if (snapshot.ReferenceMarketPrice.HasValue) score += 25;
        score += 10;

        if (snapshot.SoldCount == 0)
        {
            var cap = snapshot.ListingCount > 0 && snapshot.ReferenceMarketPrice.HasValue
                ? 55m
                : snapshot.ListingCount > 0
                    ? 45m
                    : 40m;
            score = Math.Min(score, cap);
        }

        return Math.Clamp(score, 0, 100);
    }
}

public sealed class MarketSummaryService(CardsDbContext db) : IMarketSummaryService
{
    public async Task<ProductMarketSummaryDto?> GetSummaryAsync(Guid catalogProductId, CancellationToken ct)
    {
        var product = await db.CatalogProducts.Include(p => p.Game).Include(p => p.CardSet).FirstOrDefaultAsync(p => p.Id == catalogProductId, ct);
        if (product == null) return null;
        var hasRealEvidence = await HasRealEvidenceAsync(catalogProductId, ct);
        var hasDemoEvidence = await HasDemoEvidenceAsync(catalogProductId, ct);
        var excludedCount = await db.CatalogMarketplaceListings.CountAsync(l => l.CatalogProductId == product.Id && l.IsExcludedFromMarketValue, ct);

        if (!hasRealEvidence)
        {
            return NoDataSummary(product, excludedCount, hasDemoEvidence);
        }

        var metric = await db.CatalogMarketMetrics.Where(m => m.CatalogProductId == catalogProductId).OrderByDescending(m => m.ComputedAtUtc).FirstOrDefaultAsync(ct);
        var snapshot = await MarketDataProvenance.CustomerSnapshots(db.CatalogMarketPriceSnapshots.Where(s => s.CatalogProductId == catalogProductId)).OrderByDescending(s => s.CapturedAtUtc).FirstOrDefaultAsync(ct);
        var latestReference = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).OrderByDescending(r => r.CapturedAtUtc).FirstOrDefaultAsync(ct);
        var referenceCount = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).CountAsync(ct);
        var referenceCurrent = latestReference == null ? null : MarketDataProvenance.ReferenceValue(latestReference);
        var current = metric?.CurrentMarketPrice ?? snapshot?.MedianSoldPrice ?? snapshot?.ReferenceMarketPrice ?? snapshot?.MedianListingPrice ?? snapshot?.LowestListingPrice ?? referenceCurrent;
        var confidence = current.HasValue ? metric?.ConfidenceScore ?? (snapshot == null ? ScoreReferenceOnlyConfidence(referenceCount) : MarketMetricsService.ScoreConfidence(snapshot)) : 0;
        var lastUpdated = metric?.ComputedAtUtc ?? snapshot?.CapturedAtUtc ?? latestReference?.CapturedAtUtc;
        var includedCount = (snapshot?.ListingCount ?? 0) + (snapshot?.SoldCount ?? 0) + referenceCount;
        return new ProductMarketSummaryDto
        {
            CatalogProductId = product.Id,
            ProductName = product.Name,
            GameName = product.Game?.Name ?? "",
            SetName = product.CardSet?.Name,
            CardNumber = product.CardNumber,
            ImageUrl = product.ImageUrl ?? "",
            CurrentMarketPrice = current,
            PreviousMarketPrice = metric?.PreviousMarketPrice,
            PriceChangeAmount = metric?.PriceChangeAmount,
            PriceChangePercent = metric?.PriceChangePercent,
            LowPrice = metric?.LowPrice ?? snapshot?.LowestSoldPrice ?? snapshot?.LowestListingPrice ?? snapshot?.ReferenceLowPrice ?? latestReference?.LowPrice,
            HighPrice = metric?.HighPrice ?? snapshot?.HighestSoldPrice ?? snapshot?.HighestListingPrice ?? snapshot?.ReferenceHighPrice ?? latestReference?.HighPrice,
            ListingCount = metric?.ListingCount ?? snapshot?.ListingCount ?? 0,
            SoldCount = metric?.SoldCount ?? snapshot?.SoldCount ?? 0,
            SalesVolume = metric?.SalesVolume ?? snapshot?.SalesVolume,
            EstimatedGrossMargin = metric?.EstimatedGrossMargin,
            EstimatedNetMargin = metric?.EstimatedNetMargin,
            EstimatedRoiPercent = metric?.EstimatedRoiPercent,
            DealScore = metric?.DealScore,
            OpportunityScore = metric?.OpportunityScore,
            ConfidenceScore = confidence,
            ConfidenceLabel = ConfidenceLabel(confidence),
            FreshnessLabel = FreshnessLabel(lastUpdated),
            LastUpdatedUtc = lastUpdated,
            IncludedComparableCount = includedCount,
            ExcludedComparableCount = excludedCount,
            HasMarketData = current.HasValue,
            DataStatus = DataStatus(current, snapshot, referenceCount, includedCount),
            DataQualityMessage = DataQualityMessage(current, snapshot, referenceCount, includedCount),
            IsDemoData = false
        };
    }

    private async Task<bool> HasRealEvidenceAsync(Guid catalogProductId, CancellationToken ct)
    {
        var refs = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).AnyAsync(ct);
        if (refs) return true;
        var listings = await MarketDataProvenance.RealListings(db.CatalogMarketplaceListings.Where(l => l.CatalogProductId == catalogProductId)).AnyAsync(ct);
        if (listings) return true;
        return await MarketDataProvenance.RealSales(db.CatalogMarketplaceSales.Where(s => s.CatalogProductId == catalogProductId)).AnyAsync(ct);
    }

    private async Task<bool> HasDemoEvidenceAsync(Guid catalogProductId, CancellationToken ct)
        => await db.CatalogPriceReferenceSnapshots.AnyAsync(r => r.CatalogProductId == catalogProductId && (r.SourceName == MarketDataProvenance.MockMarketSource || (r.RawSourceJson != null && r.RawSourceJson.Contains("demo"))), ct)
            || await db.CatalogMarketplaceListings.AnyAsync(l => l.CatalogProductId == catalogProductId && (l.SourceName == MarketDataProvenance.MockMarketSource || (l.RawSourceJson != null && l.RawSourceJson.Contains("demo"))), ct)
            || await db.CatalogMarketplaceSales.AnyAsync(s => s.CatalogProductId == catalogProductId && (s.SourceName == MarketDataProvenance.MockMarketSource || (s.RawSourceJson != null && s.RawSourceJson.Contains("demo"))), ct);

    private static ProductMarketSummaryDto NoDataSummary(CatalogProduct product, int excludedCount, bool hasDemoEvidence) => new()
    {
        CatalogProductId = product.Id,
        ProductName = product.Name,
        GameName = product.Game?.Name ?? "",
        SetName = product.CardSet?.Name,
        CardNumber = product.CardNumber,
        ImageUrl = product.ImageUrl ?? "",
        ConfidenceLabel = "Insufficient",
        ConfidenceScore = 0,
        FreshnessLabel = "Cold",
        ExcludedComparableCount = excludedCount,
        HasMarketData = false,
        DataStatus = hasDemoEvidence ? "Only demo data present" : "Needs refresh",
        DataQualityMessage = hasDemoEvidence
            ? "Only mock/demo rows are available for this product, so marketplace intelligence is hidden until a real provider refresh succeeds."
            : "No real marketplace/reference rows have been captured for this product yet.",
        IsDemoData = hasDemoEvidence
    };

    internal static string ConfidenceLabel(decimal? score)
        => score switch { >= 80 => "High", >= 55 => "Medium", > 0 => "Low", _ => "Insufficient" };

    internal static decimal ScoreReferenceOnlyConfidence(int referenceSourceCount)
        => referenceSourceCount switch { >= 3 => 50, 2 => 42, 1 => 35, _ => 0 };

    private static string DataStatus(decimal? current, CatalogMarketPriceSnapshot? snapshot, int referenceCount, int includedCount)
    {
        if (!current.HasValue) return "Needs refresh";
        if (snapshot?.SoldCount > 0) return "Ready";
        if (snapshot?.ListingCount > 0) return "Listing signal";
        return includedCount > referenceCount ? "Needs review" : "Reference only";
    }

    private static string DataQualityMessage(decimal? current, CatalogMarketPriceSnapshot? snapshot, int referenceCount, int includedCount)
    {
        if (!current.HasValue)
        {
            return "Provider rows exist, but no usable market price was computed yet.";
        }

        if (snapshot?.SoldCount > 0)
        {
            return "Computed from non-mock sold comps, active listings, and reference prices captured for this product.";
        }

        if (snapshot?.ListingCount > 0)
        {
            return "Computed from active listings and reference prices. Sold comps are missing, so confidence is capped until real sale evidence is captured.";
        }

        if (referenceCount > 0 || includedCount > 0)
        {
            return "A real provider reference price was captured. Add active listings and sold comps before treating it as a strong market signal.";
        }

        return "No real marketplace/reference rows have been captured for this product yet.";
    }

    internal static string FreshnessLabel(DateTime? lastUpdatedUtc)
    {
        if (!lastUpdatedUtc.HasValue) return "Cold";
        var age = DateTime.UtcNow - lastUpdatedUtc.Value;
        if (age.TotalHours < 1) return "Live";
        if (age.TotalHours < 24) return "Fresh";
        if (age.TotalHours < 72) return "Aging";
        if (age.TotalDays < 7) return "Stale";
        return "Cold";
    }
}

public sealed class MarketConfidenceService(CardsDbContext db) : IMarketConfidenceService
{
    public async Task<MarketConfidenceDto> ComputeConfidenceAsync(Guid catalogProductId, CancellationToken ct)
    {
        var snapshot = await MarketDataProvenance.CustomerSnapshots(db.CatalogMarketPriceSnapshots.Where(s => s.CatalogProductId == catalogProductId)).OrderByDescending(s => s.CapturedAtUtc).FirstOrDefaultAsync(ct);
        var referenceSources = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).Select(r => r.SourceName).Distinct().CountAsync(ct);
        var activeListings = await MarketDataProvenance.RealListings(db.CatalogMarketplaceListings.Where(l => l.CatalogProductId == catalogProductId && l.IsActive && !l.IsExcludedFromMarketValue)).CountAsync(ct);
        var soldComps = await MarketDataProvenance.RealSales(db.CatalogMarketplaceSales.Where(s => s.CatalogProductId == catalogProductId && !s.IsExcludedFromMarketValue)).CountAsync(ct);
        var score = snapshot == null ? MarketSummaryService.ScoreReferenceOnlyConfidence(referenceSources) : MarketMetricsService.ScoreConfidence(snapshot);
        var lastReferenceUpdate = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).MaxAsync(r => (DateTime?)r.CapturedAtUtc, ct);
        return new MarketConfidenceDto
        {
            Label = MarketSummaryService.ConfidenceLabel(score),
            Score = score,
            ActiveListingCount = activeListings,
            ReferenceSourceCount = referenceSources,
            SoldCompCount = soldComps,
            LastUpdatedUtc = snapshot?.CapturedAtUtc ?? lastReferenceUpdate,
            Notes = BuildConfidenceNotes(snapshot, referenceSources, activeListings, soldComps)
        };
    }

    private static string BuildConfidenceNotes(CatalogMarketPriceSnapshot? snapshot, int referenceSources, int activeListings, int soldComps)
    {
        if (soldComps > 0)
        {
            return "Computed from sold comps, active listings, reference prices, and freshness.";
        }

        if (activeListings > 0)
        {
            return "Active-listing signal only. Sold comps are missing, so confidence is capped until real sales data is available.";
        }

        if (referenceSources > 0 || snapshot?.ReferenceMarketPrice.HasValue == true)
        {
            return "Reference-only signal. Add active listings and sold comps to raise confidence.";
        }

        return "No real provider evidence has been captured yet.";
    }
}

public sealed class MarketChartService(CardsDbContext db) : IMarketChartService
{
    public async Task<MarketChartDto> GetMarketChartAsync(Guid catalogProductId, MarketChartRequest request, CancellationToken ct)
    {
        var hasRealEvidence =
            await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).AnyAsync(ct)
            || await MarketDataProvenance.RealListings(db.CatalogMarketplaceListings.Where(l => l.CatalogProductId == catalogProductId)).AnyAsync(ct)
            || await MarketDataProvenance.RealSales(db.CatalogMarketplaceSales.Where(s => s.CatalogProductId == catalogProductId)).AnyAsync(ct);
        if (!hasRealEvidence)
        {
            return new MarketChartDto { CatalogProductId = catalogProductId };
        }

        var snapshots = await MarketDataProvenance.CustomerSnapshots(db.CatalogMarketPriceSnapshots.Where(s => s.CatalogProductId == catalogProductId)).OrderBy(s => s.CapturedAtUtc).ToListAsync(ct);
        if (snapshots.Count == 0)
        {
            var referencePoints = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId))
                .OrderBy(r => r.CapturedAtUtc)
                .ToListAsync(ct);
            var referencePrices = referencePoints.Select(MarketDataProvenance.ReferenceValue).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
            return new MarketChartDto
            {
                CatalogProductId = catalogProductId,
                PriceSeries = referencePoints.Select(r => new MarketChartPointDto
                {
                    DateUtc = r.CapturedAtUtc.Date,
                    ReferencePrice = MarketDataProvenance.ReferenceValue(r),
                    LowPrice = r.LowPrice,
                    HighPrice = r.HighPrice
                }).ToArray(),
                VolumeSeries = referencePoints.Select(r => new MarketVolumePointDto { DateUtc = r.CapturedAtUtc.Date, ListingCount = 0, SoldCount = 0 }).ToArray(),
                DistributionBuckets = BuildBuckets(referencePrices),
                PercentileMarkers = BuildPercentiles(referencePrices)
            };
        }

        var prices = snapshots.Select(s => s.ReferenceMarketPrice ?? s.MedianListingPrice ?? s.LowestListingPrice).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
        return new MarketChartDto
        {
            CatalogProductId = catalogProductId,
            PriceSeries = snapshots.Select(s => new MarketChartPointDto
            {
                DateUtc = s.CapturedAtUtc.Date,
                ReferencePrice = s.ReferenceMarketPrice,
                MedianActiveListing = s.MedianListingPrice,
                LowestActiveListing = s.LowestListingPrice,
                MedianSoldPrice = s.MedianSoldPrice,
                LowPrice = s.LowestListingPrice ?? s.ReferenceLowPrice,
                HighPrice = s.HighestListingPrice ?? s.ReferenceHighPrice
            }).ToArray(),
            VolumeSeries = snapshots.Select(s => new MarketVolumePointDto { DateUtc = s.CapturedAtUtc.Date, ListingCount = s.ListingCount, SoldCount = s.SoldCount, SalesVolume = s.SalesVolume }).ToArray(),
            DistributionBuckets = BuildBuckets(prices),
            PercentileMarkers = BuildPercentiles(prices)
        };
    }

    private static IReadOnlyList<MarketDistributionBucketDto> BuildBuckets(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0) return Array.Empty<MarketDistributionBucketDto>();
        var min = values.Min();
        var max = values.Max();
        var width = Math.Max(0.01m, (max - min) / 8m);
        return Enumerable.Range(0, 8).Select(i =>
        {
            var low = min + width * i;
            var high = i == 7 ? max : low + width;
            return new MarketDistributionBucketDto { MinPrice = decimal.Round(low, 2), MaxPrice = decimal.Round(high, 2), Count = values.Count(v => v >= low && v <= high) };
        }).ToArray();
    }

    private static IReadOnlyList<MarketPercentileMarkerDto> BuildPercentiles(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0) return Array.Empty<MarketPercentileMarkerDto>();
        var ordered = values.OrderBy(v => v).ToArray();
        return new[] { 5, 10, 25, 50, 75, 90, 95 }.Select(p =>
        {
            var index = (int)Math.Floor((p / 100m) * (ordered.Length - 1));
            return new MarketPercentileMarkerDto { Percentile = p, Price = ordered[index] };
        }).ToArray();
    }
}

public sealed class MarketplaceComparisonService(CardsDbContext db) : IMarketplaceComparisonService
{
    public async Task<MarketplaceComparisonDto> GetComparisonAsync(Guid catalogProductId, MarketplaceComparisonRequest request, CancellationToken ct)
    {
        var rows = new Dictionary<string, MarketplaceComparisonRowDto>(StringComparer.OrdinalIgnoreCase);
        var refs = await MarketDataProvenance.RealReferenceSnapshots(db.CatalogPriceReferenceSnapshots.Where(r => r.CatalogProductId == catalogProductId)).GroupBy(r => r.SourceName).ToListAsync(ct);
        foreach (var group in refs)
        {
            var latest = group.OrderByDescending(r => r.CapturedAtUtc).First();
            var row = GetOrCreate(rows, group.Key);
            row.ReferenceMarketPrice = latest.MarketPrice ?? latest.MidPrice ?? latest.UngradedPrice;
            row.FreshnessLabel = MarketSummaryService.FreshnessLabel(latest.CapturedAtUtc);
        }

        var listings = await MarketDataProvenance.RealListings(db.CatalogMarketplaceListings.Where(l => l.CatalogProductId == catalogProductId && l.IsActive && !l.IsExcludedFromMarketValue)).GroupBy(l => l.SourceName).ToListAsync(ct);
        foreach (var group in listings)
        {
            var effective = group.Select(l => l.EffectivePrice).OrderBy(v => v).ToArray();
            var row = GetOrCreate(rows, group.Key);
            row.LowestActiveListing = effective.FirstOrDefault();
            row.MedianActiveListing = MarketAggregationService.Median(effective);
            row.ListingCount = effective.Length;
            row.FreshnessLabel = MostRecentFreshness(row.FreshnessLabel, group.Max(l => l.LastSeenUtc));
            row.ExternalUrl = group.OrderBy(l => l.EffectivePrice).FirstOrDefault()?.ListingUrl;
        }

        var sales = await MarketDataProvenance.RealSales(db.CatalogMarketplaceSales.Where(s => s.CatalogProductId == catalogProductId && !s.IsExcludedFromMarketValue)).GroupBy(s => s.SourceName).ToListAsync(ct);
        foreach (var group in sales)
        {
            var row = GetOrCreate(rows, group.Key);
            row.LastSoldPrice = group.OrderByDescending(s => s.SoldAtUtc).Select(s => (decimal?)s.EffectiveSoldPrice).FirstOrDefault();
            row.SoldCount = group.Count();
            row.SalesVolume = group.Sum(s => s.EffectiveSoldPrice);
            row.FreshnessLabel = MostRecentFreshness(row.FreshnessLabel, group.Max(s => s.CapturedAtUtc));
        }

        foreach (var row in rows.Values)
        {
            if (row.LowestActiveListing.HasValue && row.ReferenceMarketPrice is > 0)
            {
                row.SpreadAmount = row.LowestActiveListing.Value - row.ReferenceMarketPrice.Value;
                row.SpreadPercent = row.SpreadAmount.Value / row.ReferenceMarketPrice.Value * 100m;
            }

            row.ConfidenceLabel = row switch
            {
                { ListingCount: >= 20, SoldCount: > 0 } => "High",
                { ListingCount: >= 5 } or { SoldCount: > 0 } or { ReferenceMarketPrice: > 0 } => "Medium",
                _ => "Low"
            };
        }

        return new MarketplaceComparisonDto
        {
            CatalogProductId = catalogProductId,
            Rows = rows.Values
                .OrderBy(r => r.SourceName == "TCGplayer" ? 0 : r.SourceName == "PokemonTCG" ? 1 : 2)
                .ThenBy(r => r.SourceName)
                .ToArray()
        };
    }

    private static MarketplaceComparisonRowDto GetOrCreate(Dictionary<string, MarketplaceComparisonRowDto> rows, string sourceName)
    {
        if (!rows.TryGetValue(sourceName, out var row))
        {
            row = new MarketplaceComparisonRowDto { SourceName = sourceName, FreshnessLabel = "Cold" };
            rows[sourceName] = row;
        }

        return row;
    }

    private static string MostRecentFreshness(string currentLabel, DateTime candidate)
    {
        var next = MarketSummaryService.FreshnessLabel(candidate);
        if (currentLabel == "Cold") return next;
        return FreshnessRank(next) > FreshnessRank(currentLabel) ? next : currentLabel;
    }

    private static int FreshnessRank(string label)
        => label switch { "Live" => 5, "Fresh" => 4, "Aging" => 3, "Stale" => 2, _ => 1 };
}

public sealed class DealScannerService(CardsDbContext db, IMarketSummaryService summaries) : IDealScannerService
{
    public async Task<IReadOnlyList<DealOpportunityDto>> GetDealsAsync(DealScanRequest request, CancellationToken ct)
    {
        var query = MarketDataProvenance.RealListings(db.CatalogMarketplaceListings.Include(l => l.CatalogProduct).ThenInclude(p => p!.Game).Include(l => l.CatalogProduct).ThenInclude(p => p!.CardSet).Where(l => l.IsActive && !l.IsExcludedFromMarketValue));
        if (request.CatalogProductId.HasValue) query = query.Where(l => l.CatalogProductId == request.CatalogProductId.Value);
        if (request.CardSetId.HasValue) query = query.Where(l => l.CatalogProduct != null && l.CatalogProduct.CardSetId == request.CardSetId.Value);
        if (!string.IsNullOrWhiteSpace(request.GameSlug)) query = query.Where(l => l.CatalogProduct != null && l.CatalogProduct.Game != null && l.CatalogProduct.Game.Slug == request.GameSlug);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var search = request.Query.ToLower();
            query = query.Where(l =>
                l.Title.ToLower().Contains(search)
                || (l.CatalogProduct != null && l.CatalogProduct.Name.ToLower().Contains(search))
                || (l.CatalogProduct != null && l.CatalogProduct.CardNumber != null && l.CatalogProduct.CardNumber.ToLower().Contains(search)));
        }
        if (request.MaxListingPrice.HasValue) query = query.Where(l => l.EffectivePrice <= request.MaxListingPrice.Value);
        if (request.MinConfidence.HasValue) query = query.Where(l => (l.MatchConfidence ?? 0m) >= NormalizeConfidenceFilter(request.MinConfidence.Value));

        var listings = await query.OrderBy(l => l.EffectivePrice).Take(500).ToListAsync(ct);
        var deals = new List<DealOpportunityDto>();
        foreach (var listing in listings)
        {
            var summary = await summaries.GetSummaryAsync(listing.CatalogProductId, ct);
            if (summary?.CurrentMarketPrice is not > 0) continue;
            if (request.MinMarketValue.HasValue && summary.CurrentMarketPrice.Value < request.MinMarketValue.Value) continue;
            var discountPercent = (summary.CurrentMarketPrice.Value - listing.EffectivePrice) / summary.CurrentMarketPrice.Value * 100m;
            if (discountPercent < request.ThresholdPercent) continue;
            var deal = ToDeal(listing, summary, discountPercent);
            if (request.MinRoiPercent.HasValue && (deal.EstimatedRoiPercent ?? decimal.MinValue) < request.MinRoiPercent.Value) continue;
            deals.Add(deal);
        }

        return deals.OrderByDescending(d => d.DealScore).Take(Math.Clamp(request.Take, 1, 100)).ToArray();
    }

    public Task<DealScoreDto> ScoreListingAsync(CatalogMarketplaceListing listing, CancellationToken ct)
    {
        var score = listing.MatchConfidence is >= 0.90m ? 70m : 35m;
        if (listing.ShippingPrice is > 8m) score -= 10m;
        if (listing.IsAuction && listing.AuctionEndsUtc < DateTime.UtcNow.AddDays(1)) score += 8m;
        score = Math.Clamp(score, 0, 100);
        return Task.FromResult(new DealScoreDto { Score = score, Label = Label(score), Notes = "Listing scored from confidence, shipping, and auction urgency." });
    }

    private static DealOpportunityDto ToDeal(CatalogMarketplaceListing listing, ProductMarketSummaryDto summary, decimal discountPercent)
    {
        var market = summary.CurrentMarketPrice ?? 0m;
        var estimatedFees = EstimateResaleFees(market);
        var estimatedNetProfit = market - estimatedFees - listing.EffectivePrice;
        decimal? estimatedRoi = listing.EffectivePrice > 0 ? estimatedNetProfit / listing.EffectivePrice * 100m : null;
        var confidenceComponent = (summary.ConfidenceScore ?? 0m) * 0.35m;
        var liquidityScore = Math.Clamp((summary.SoldCount * 8m) + (summary.ListingCount * 1.5m) + confidenceComponent, 0m, 100m);
        var score = Math.Clamp(discountPercent * 1.6m + ((listing.MatchConfidence ?? 0.80m) * 30m) + ((summary.ConfidenceScore ?? 0m) * 0.20m), 0, 100);
        return new DealOpportunityDto
        {
            ListingId = listing.Id,
            CatalogProductId = listing.CatalogProductId,
            ProductName = listing.CatalogProduct?.Name ?? "",
            GameName = listing.CatalogProduct?.Game?.Name ?? "",
            SetName = listing.CatalogProduct?.CardSet?.Name,
            CardSetId = listing.CatalogProduct?.CardSetId,
            CardNumber = listing.CatalogProduct?.CardNumber,
            SourceName = listing.SourceName,
            Title = listing.Title,
            ItemPrice = listing.Price,
            ShippingPrice = listing.ShippingPrice,
            ListingPrice = listing.EffectivePrice,
            TrustedMarketPrice = market,
            ExpectedMarketValue = market,
            EstimatedFees = estimatedFees,
            EstimatedNetProfit = estimatedNetProfit,
            EstimatedRoiPercent = estimatedRoi,
            LiquidityScore = liquidityScore,
            MatchConfidence = listing.MatchConfidence,
            DiscountAmount = market - listing.EffectivePrice,
            DiscountPercent = discountPercent,
            DealScore = score,
            DealLabel = Label(score),
            Reason = BuildReason(discountPercent, estimatedNetProfit, estimatedRoi, listing.MatchConfidence, summary),
            ListingUrl = listing.ListingUrl,
            ImageUrl = listing.ImageUrl
        };
    }

    private static string Label(decimal score) => score switch { >= 85 => "Excellent", >= 70 => "Strong", >= 50 => "Good", _ => "Weak" };

    private static decimal NormalizeConfidenceFilter(decimal confidence)
        => confidence > 1m ? confidence / 100m : confidence;

    private static decimal EstimateResaleFees(decimal expectedMarketValue)
        => Math.Round((expectedMarketValue * 0.1325m) + 0.40m, 2);

    private static string BuildReason(decimal discountPercent, decimal estimatedNetProfit, decimal? estimatedRoi, decimal? matchConfidence, ProductMarketSummaryDto summary)
    {
        var confidence = matchConfidence.HasValue ? $"{NormalizeConfidenceDisplay(matchConfidence.Value)}% match" : "unscored match";
        var roi = estimatedRoi.HasValue ? $"{estimatedRoi.Value:F1}% ROI" : "ROI pending";
        return $"{discountPercent:F1}% under market; {estimatedNetProfit:C} estimated net; {roi}; {summary.ListingCount} active / {summary.SoldCount} sold; {confidence}.";
    }

    private static int NormalizeConfidenceDisplay(decimal confidence)
        => (int)Math.Round(confidence <= 1m ? confidence * 100m : confidence);
}

public sealed class SetMarketDashboardService(CardsDbContext db, IMarketSummaryService summaries, IDealScannerService deals) : ISetMarketDashboardService
{
    public async Task<SetMarketDashboardDto?> GetDashboardAsync(Guid cardSetId, CancellationToken ct)
    {
        var set = await db.CardSets.Include(s => s.Game).FirstOrDefaultAsync(s => s.Id == cardSetId, ct);
        if (set == null) return null;
        var products = await db.CatalogProducts.Where(p => p.CardSetId == cardSetId && p.IsActive).OrderBy(p => p.Name).Take(500).ToListAsync(ct);
        var rows = new List<SetMarketDashboardProductDto>();
        foreach (var product in products)
        {
            var summary = await summaries.GetSummaryAsync(product.Id, ct);
            if (summary == null) continue;
            rows.Add(new SetMarketDashboardProductDto
            {
                CatalogProductId = product.Id,
                ProductName = product.Name,
                GameName = set.Game?.Name ?? "",
                SetName = set.Name,
                CardNumber = product.CardNumber,
                CategoryName = product.ProductType,
                ImageUrl = product.ImageUrl,
                CurrentMarketPrice = summary.CurrentMarketPrice,
                PriceChangePercent = summary.PriceChangePercent,
                ListingCount = summary.ListingCount,
                SoldCount = summary.SoldCount,
                OpportunityScore = summary.OpportunityScore,
                RankingScore = summary.OpportunityScore ?? summary.DealScore,
                SignalLabel = summary.HasMarketData ? summary.FreshnessLabel : summary.DataStatus,
                SignalDetail = summary.DataQualityMessage,
                ConfidenceLabel = summary.ConfidenceLabel,
                HasMarketData = summary.HasMarketData,
                IsDemoData = summary.IsDemoData
            });
        }

        return new SetMarketDashboardDto
        {
            CardSetId = set.Id,
            SetName = set.Name,
            GameName = set.Game?.Name ?? "",
            Products = rows.OrderBy(r => CardNumberSortKey(r.CardNumber)).ThenBy(r => r.ProductName).ToArray(),
            TopMovers = rows.OrderByDescending(r => Math.Abs(r.PriceChangePercent ?? 0m)).Take(8).ToArray(),
            HighestVolume = rows.OrderByDescending(r => r.SoldCount + r.ListingCount).Take(8).ToArray(),
            HighestOpportunity = rows.OrderByDescending(r => r.OpportunityScore).Take(8).ToArray(),
            MostListed = rows.OrderByDescending(r => r.ListingCount).Take(8).ToArray(),
            LowestConfidence = rows.OrderBy(r => r.ConfidenceLabel == "Insufficient" ? 0 : r.ConfidenceLabel == "Low" ? 1 : 2).Take(8).ToArray(),
            BestDeals = await deals.GetDealsAsync(new DealScanRequest { CardSetId = cardSetId, Take = 8 }, ct)
        };
    }

    public async Task<SetMarketDashboardDto?> GetDashboardBySlugAsync(string gameSlug, string setSlug, CancellationToken ct)
    {
        var set = await db.CardSets.Include(s => s.Game).FirstOrDefaultAsync(s => s.Slug == setSlug && s.Game != null && s.Game.Slug == gameSlug, ct);
        return set == null ? null : await GetDashboardAsync(set.Id, ct);
    }

    private static int CardNumberSortKey(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return int.MaxValue;
        var digits = new string(cardNumber.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : int.MaxValue;
    }
}

public sealed class CatalogWatchlistService(CardsDbContext db, IMarketSummaryService summaries) : ICatalogWatchlistService
{
    public async Task<IReadOnlyList<WatchlistIntelligenceDto>> GetUserWatchlistAsync(Guid userId, CancellationToken ct)
        => await GetRows(userId, ct);

    public async Task<WatchlistIntelligenceDto> AddAsync(Guid userId, CreateCatalogWatchlistItemRequest request, CancellationToken ct)
    {
        var exists = await db.CatalogWatchlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.CatalogProductId == request.CatalogProductId && w.ProductVariantId == request.ProductVariantId, ct);
        if (exists == null)
        {
            exists = new CatalogWatchlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CatalogProductId = request.CatalogProductId,
                ProductVariantId = request.ProductVariantId,
                TargetPrice = request.TargetPrice,
                TargetDiscountPercent = request.TargetDiscountPercent,
                AlertOnDataRefresh = request.AlertOnDataRefresh,
                AlertOnNewDeal = request.AlertOnNewDeal,
                AlertOnPriceDrop = request.AlertOnPriceDrop,
                AlertOnVolumeSpike = request.AlertOnVolumeSpike,
                Notes = request.Notes,
                CreatedUtc = DateTime.UtcNow
            };
            db.CatalogWatchlistItems.Add(exists);
            await db.SaveChangesAsync(ct);
        }
        return (await GetRows(userId, ct)).First(w => w.WatchlistItemId == exists.Id);
    }

    public async Task RemoveAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var row = await db.CatalogWatchlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.Id == id, ct);
        if (row != null)
        {
            db.CatalogWatchlistItems.Remove(row);
            await db.SaveChangesAsync(ct);
        }
    }

    public Task<IReadOnlyList<WatchlistIntelligenceDto>> GetIntelligenceAsync(Guid userId, CancellationToken ct)
        => GetUserWatchlistAsync(userId, ct);

    private async Task<IReadOnlyList<WatchlistIntelligenceDto>> GetRows(Guid userId, CancellationToken ct)
    {
        var items = await db.CatalogWatchlistItems.Include(w => w.CatalogProduct).Where(w => w.UserId == userId).OrderByDescending(w => w.CreatedUtc).ToListAsync(ct);
        var rows = new List<WatchlistIntelligenceDto>();
        foreach (var item in items)
        {
            var summary = await summaries.GetSummaryAsync(item.CatalogProductId, ct);
            rows.Add(new WatchlistIntelligenceDto
            {
                WatchlistItemId = item.Id,
                CatalogProductId = item.CatalogProductId,
                ProductName = item.CatalogProduct?.Name ?? "",
                SignalLabel = summary?.CurrentMarketPrice <= item.TargetPrice ? "Target reached" : "Watching",
                SignalDetail = summary == null ? "Market data not available yet." : $"{summary.FreshnessLabel} data, {summary.ConfidenceLabel} confidence.",
                CurrentMarketPrice = summary?.CurrentMarketPrice,
                TargetPrice = item.TargetPrice,
                OpportunityScore = summary?.OpportunityScore,
                CreatedUtc = item.CreatedUtc
            });
        }
        return rows;
    }
}

public sealed class MarketProviderHealthService(
    IEnumerable<IMarketplaceReferencePriceProvider> referenceProviders,
    IEnumerable<IMarketplaceActiveListingProvider> listingProviders,
    IEnumerable<IMarketplaceSoldCompsProvider> soldProviders) : IMarketProviderHealthService
{
    public Task<IReadOnlyList<ProviderHealthDto>> GetHealthAsync(CancellationToken ct)
    {
        var rows = referenceProviders.Select(p => Row(p.SourceName, "ReferencePrice", p.IsEnabled))
            .Concat(listingProviders.Select(p => Row(p.SourceName, "ActiveListing", p.IsEnabled)))
            .Concat(soldProviders.Select(p => Row(p.SourceName, "SoldComps", p.IsEnabled)))
            .GroupBy(r => $"{r.SourceName}:{r.ProviderType}")
            .Select(g => g.First())
            .OrderBy(r => r.SourceName)
            .ThenBy(r => r.ProviderType)
            .ToArray();
        return Task.FromResult<IReadOnlyList<ProviderHealthDto>>(rows);
    }

    private static ProviderHealthDto Row(string source, string type, bool enabled) => new()
    {
        SourceName = source,
        ProviderType = type,
        IsEnabled = enabled,
        IsHealthy = true,
        Status = enabled ? "Ready" : "Disabled",
        Message = Message(source, type, enabled)
    };

    private static string Message(string source, string type, bool enabled)
    {
        if (enabled)
        {
            return "Provider is enabled.";
        }

        if (source.Equals("eBay", StringComparison.OrdinalIgnoreCase) && type.Equals("SoldComps", StringComparison.OrdinalIgnoreCase))
        {
            return "Sold comps are disabled: the current eBay Browse integration only supports active listings. Add an approved sold-comps source before treating deal confidence as strong.";
        }

        return "Provider scaffold is present but disabled by configuration.";
    }
}
