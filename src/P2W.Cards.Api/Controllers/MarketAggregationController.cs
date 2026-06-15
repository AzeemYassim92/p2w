using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/aggregation")]
public sealed class MarketAggregationController(IMarketAggregationService aggregation) : ControllerBase
{
    [HttpPost("products/{catalogProductId:guid}/refresh")]
    public async Task<ActionResult<MarketAggregationResultDto>> RefreshProduct(Guid catalogProductId, MarketRefreshRequest request, CancellationToken ct)
    {
        try { return await aggregation.RefreshProductMarketDataAsync(catalogProductId, request, ct); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("sets/{cardSetId:guid}/refresh")]
    public Task<MarketAggregationResultDto> RefreshSet(Guid cardSetId, MarketRefreshRequest request, CancellationToken ct)
        => aggregation.RefreshSetMarketDataAsync(cardSetId, request, ct);

    [HttpPost("recently-viewed/refresh")]
    public Task<MarketAggregationResultDto> RefreshRecentlyViewed(MarketRefreshRequest request, CancellationToken ct)
        => aggregation.RefreshRecentlyViewedAsync(request, ct);

    [HttpPost("watchlisted/refresh")]
    public Task<MarketAggregationResultDto> RefreshWatchlisted(MarketRefreshRequest request, CancellationToken ct)
        => aggregation.RefreshWatchlistedAsync(request, ct);

    [HttpPost("trending/refresh")]
    public Task<MarketAggregationResultDto> RefreshTrending(MarketRefreshRequest request, CancellationToken ct)
        => aggregation.RefreshTrendingAsync(request, ct);

    [HttpGet("runs")]
    public Task<IReadOnlyList<MarketAggregationResultDto>> GetRuns([FromQuery] int take = 25, CancellationToken ct = default)
        => aggregation.GetRunsAsync(take, ct);

    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<MarketAggregationResultDto>> GetRun(Guid runId, CancellationToken ct)
    {
        var run = await aggregation.GetRunAsync(runId, ct);
        return run == null ? NotFound() : run;
    }
}
