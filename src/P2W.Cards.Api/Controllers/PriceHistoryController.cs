using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.Common;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/cards/{cardId:guid}/price-history")]
public sealed class PriceHistoryController(IPriceHistoryService prices) : ControllerBase
{
    [HttpGet("listings")]
    public async Task<IActionResult> ListingHistory(Guid cardId, CancellationToken ct) => Ok(await prices.GetListingPriceHistoryAsync(cardId, ct));

    [HttpGet("references")]
    public async Task<IActionResult> ReferenceHistory(Guid cardId, CancellationToken ct) => Ok(await prices.GetReferencePriceHistoryAsync(cardId, ct));

    [HttpPost("capture-listing-snapshot")]
    public async Task<IActionResult> Capture(Guid cardId, CancellationToken ct)
    {
        await prices.CaptureListingSnapshotForCardAsync(cardId, ct);
        return NoContent();
    }

    [HttpPost("refresh-reference-prices")]
    public async Task<IActionResult> Refresh(Guid cardId, CancellationToken ct)
    {
        try
        {
            await prices.RefreshReferencePricesForCardAsync(cardId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse { Error = "Card not found", Code = "CARD_NOT_FOUND" });
        }
    }
}
