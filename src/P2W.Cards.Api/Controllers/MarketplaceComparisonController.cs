using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/products/{catalogProductId:guid}/comparison")]
public sealed class MarketplaceComparisonController(IMarketplaceComparisonService comparison) : ControllerBase
{
    [HttpGet]
    public Task<MarketplaceComparisonDto> Get(Guid catalogProductId, [FromQuery] MarketplaceComparisonRequest request, CancellationToken ct)
        => comparison.GetComparisonAsync(catalogProductId, request, ct);
}
