using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/catalog/products/{productId:guid}/pricing")]
public sealed class CatalogPricingController(ICatalogPricingService pricing) : ControllerBase
{
    [HttpGet("history")]
    public Task<IReadOnlyList<CatalogPriceReferenceSnapshotDto>> GetHistory(Guid productId, CancellationToken ct)
        => pricing.GetPriceHistoryAsync(productId, ct);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(Guid productId, CancellationToken ct)
    {
        try
        {
            await pricing.RefreshPricesForProductAsync(productId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
