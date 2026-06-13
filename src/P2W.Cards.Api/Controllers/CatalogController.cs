using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(ICatalogService catalog) : ControllerBase
{
    [HttpGet("games")]
    public Task<IReadOnlyList<GameDto>> GetGames([FromQuery] bool primaryOnly = false, CancellationToken ct = default)
        => catalog.GetGamesAsync(primaryOnly, ct);

    [HttpGet("sets")]
    public Task<IReadOnlyList<CardSetDto>> GetSets([FromQuery] Guid? gameId, [FromQuery] string? gameSlug, [FromQuery] bool? upcoming, [FromQuery] int take = 24, CancellationToken ct = default)
        => catalog.GetSetsAsync(gameId, gameSlug, upcoming, take, ct);

    [HttpGet("categories")]
    public Task<IReadOnlyList<ProductCategoryDto>> GetCategories(CancellationToken ct)
        => catalog.GetCategoriesAsync(ct);

    [HttpGet("products")]
    public Task<IReadOnlyList<CatalogProductDto>> GetProducts([FromQuery] CatalogProductQuery query, CancellationToken ct)
        => catalog.GetProductsAsync(query, ct);

    [HttpGet("products/{productId:guid}")]
    public async Task<ActionResult<CatalogProductDetailDto>> GetProduct(Guid productId, CancellationToken ct)
    {
        var product = await catalog.GetProductDetailAsync(productId, ct);
        return product == null ? NotFound() : product;
    }

    [HttpGet("providers/capabilities")]
    public Task<IReadOnlyList<ProviderCapabilityDto>> GetProviderCapabilities(CancellationToken ct)
        => catalog.GetProviderCapabilitiesAsync(ct);
}
