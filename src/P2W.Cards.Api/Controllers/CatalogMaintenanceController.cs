using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/catalog/maintenance")]
public sealed class CatalogMaintenanceController(ICatalogMaintenanceService maintenance) : ControllerBase
{
    [HttpGet("completeness")]
    public Task<CatalogCompletenessDto> Completeness([FromQuery] string gameSlug = "pokemon", CancellationToken ct = default)
        => maintenance.GetCompletenessAsync(gameSlug, ct);

    [HttpPost("pokemon/backfill")]
    public async Task<ActionResult<CatalogMetadataBackfillResultDto>> BackfillPokemon(CatalogMetadataBackfillRequest request, CancellationToken ct)
    {
        try
        {
            return await maintenance.BackfillMetadataAsync(request, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
