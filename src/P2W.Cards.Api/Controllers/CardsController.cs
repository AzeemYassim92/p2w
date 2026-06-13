using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.Common;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/cards")]
public sealed class CardsController(ICardSearchService cards) : ControllerBase
{
    [HttpGet("featured")]
    public async Task<IActionResult> Featured(CancellationToken ct, [FromQuery] string? productType, [FromQuery] int take = 10)
    {
        return Ok(await cards.GetFeaturedMarketplaceProductsAsync(productType, take, ct));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? game, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new ApiErrorResponse { Error = "Query is required.", Code = "QUERY_REQUIRED" });
        }

        try
        {
            return Ok(await cards.SearchCardsAsync(query, game, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse { Error = ex.Message, Code = "INVALID_GAME" });
        }
    }

    [HttpGet("{cardId:guid}")]
    public async Task<IActionResult> Detail(Guid cardId, CancellationToken ct)
    {
        var card = await cards.GetCardDetailAsync(cardId, ct);
        return card == null ? NotFound(new ApiErrorResponse { Error = "Card not found", Code = "CARD_NOT_FOUND" }) : Ok(card);
    }
}
