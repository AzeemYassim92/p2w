using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.Common;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController(IPriceAlertService alerts, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await alerts.GetUserAlertsAsync(currentUser.UserId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePriceAlertRequest request, CancellationToken ct)
    {
        try
        {
            return Created("/api/alerts", await alerts.CreateAlertAsync(currentUser.UserId, request, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiErrorResponse { Error = "Card not found", Code = "CARD_NOT_FOUND" });
        }
    }

    [HttpPatch("{alertId:guid}/disable")]
    public async Task<IActionResult> Disable(Guid alertId, CancellationToken ct)
    {
        await alerts.DisableAlertAsync(currentUser.UserId, alertId, ct);
        return NoContent();
    }
}
