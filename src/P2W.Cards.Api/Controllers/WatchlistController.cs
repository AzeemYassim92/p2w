using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.Common;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/watchlist")]
public sealed class WatchlistController(IWatchlistService watchlist, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await watchlist.GetUserWatchlistAsync(currentUser.UserId, ct));

    [HttpPost]
    public async Task<IActionResult> Add(AddWatchlistItemRequest request, CancellationToken ct)
    {
        try
        {
            var result = await watchlist.AddToWatchlistAsync(currentUser.UserId, request, ct);
            return Created($"/api/watchlist/{result.WatchlistItemId}", result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse { Error = "Card not found", Code = "CARD_NOT_FOUND" });
        }
    }

    [HttpDelete("{watchlistItemId:guid}")]
    public async Task<IActionResult> Remove(Guid watchlistItemId, CancellationToken ct)
    {
        await watchlist.RemoveFromWatchlistAsync(currentUser.UserId, watchlistItemId, ct);
        return NoContent();
    }
}
