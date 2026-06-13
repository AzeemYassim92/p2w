using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.Common;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/cards/{cardId:guid}")]
public sealed class ListingsController(IListingService listings) : ControllerBase
{
    [HttpGet("listings")]
    public async Task<IActionResult> Get(Guid cardId, CancellationToken ct) => Ok(await listings.GetListingsForCardAsync(cardId, ct));

    [HttpPost("refresh-listings")]
    public async Task<IActionResult> Refresh(Guid cardId, CancellationToken ct)
    {
        try
        {
            await listings.RefreshListingsForCardAsync(cardId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse { Error = "Card not found", Code = "CARD_NOT_FOUND" });
        }
    }
}
