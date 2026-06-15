using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/catalog-watchlist")]
public sealed class CatalogWatchlistController(ICatalogWatchlistService watchlist, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<WatchlistIntelligenceDto>> Get(CancellationToken ct)
        => watchlist.GetUserWatchlistAsync(currentUser.UserId, ct);

    [HttpPost]
    public Task<WatchlistIntelligenceDto> Add(CreateCatalogWatchlistItemRequest request, CancellationToken ct)
        => watchlist.AddAsync(currentUser.UserId, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await watchlist.RemoveAsync(currentUser.UserId, id, ct);
        return NoContent();
    }

    [HttpGet("intelligence")]
    public Task<IReadOnlyList<WatchlistIntelligenceDto>> Intelligence(CancellationToken ct)
        => watchlist.GetIntelligenceAsync(currentUser.UserId, ct);
}
