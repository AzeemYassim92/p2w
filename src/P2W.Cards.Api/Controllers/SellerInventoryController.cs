using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/seller-inventory")]
public sealed class SellerInventoryController(ISellerInventoryService inventory, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SellerInventoryItemDto>> GetInventory(CancellationToken ct)
        => inventory.GetInventoryAsync(currentUser.UserId, ct);

    [HttpPost]
    public async Task<ActionResult<SellerInventoryItemDto>> CreateInventoryItem(CreateSellerInventoryItemRequest request, CancellationToken ct)
    {
        try
        {
            var item = await inventory.CreateInventoryItemAsync(currentUser.UserId, request, ct);
            return CreatedAtAction(nameof(GetInventory), new { id = item.SellerInventoryItemId }, item);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
