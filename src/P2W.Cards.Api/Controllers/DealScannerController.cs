using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market")]
public sealed class DealScannerController(IDealScannerService deals) : ControllerBase
{
    [HttpGet("deals")]
    public Task<IReadOnlyList<DealOpportunityDto>> GetDeals([FromQuery] DealScanRequest request, CancellationToken ct)
        => deals.GetDealsAsync(request, ct);

    [HttpGet("products/{catalogProductId:guid}/deals")]
    public Task<IReadOnlyList<DealOpportunityDto>> GetProductDeals(Guid catalogProductId, [FromQuery] int take = 25, CancellationToken ct = default)
        => deals.GetDealsAsync(new DealScanRequest { CatalogProductId = catalogProductId, Take = take }, ct);
}
