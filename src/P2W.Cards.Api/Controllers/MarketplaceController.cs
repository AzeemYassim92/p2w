using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/marketplace")]
public sealed class MarketplaceController(ICatalogDiscoveryService discovery, ICatalogService catalog) : ControllerBase
{
    [HttpGet("home")]
    public Task<MarketplaceHomeDto> GetHome([FromQuery] string? gameSlug, CancellationToken ct)
        => discovery.GetMarketplaceHomeAsync(gameSlug, ct);

    [HttpGet("featured")]
    public Task<IReadOnlyList<CatalogProductDto>> GetFeatured([FromQuery] string? gameSlug, [FromQuery] int take = 12, CancellationToken ct = default)
        => catalog.GetProductsAsync(new CatalogProductQuery { GameSlug = gameSlug, Take = take }, ct);

    [HttpGet("trending")]
    public async Task<IReadOnlyList<CatalogProductDto>> GetTrending([FromQuery] string? gameSlug, [FromQuery] int take = 12, CancellationToken ct = default)
    {
        var products = await catalog.GetProductsAsync(new CatalogProductQuery { GameSlug = gameSlug, Take = take }, ct);
        return products.Where(p => p.IsTrending).ToArray();
    }
}
