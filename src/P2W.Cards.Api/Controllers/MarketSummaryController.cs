using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/products/{catalogProductId:guid}")]
public sealed class MarketSummaryController(IMarketSummaryService summaries, IMarketConfidenceService confidence) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ProductMarketSummaryDto>> GetSummary(Guid catalogProductId, CancellationToken ct)
    {
        var summary = await summaries.GetSummaryAsync(catalogProductId, ct);
        return summary == null ? NotFound() : summary;
    }

    [HttpGet("confidence")]
    public Task<MarketConfidenceDto> GetConfidence(Guid catalogProductId, CancellationToken ct)
        => confidence.ComputeConfidenceAsync(catalogProductId, ct);
}
