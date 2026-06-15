using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/sets")]
public sealed class SetMarketDashboardController(ISetMarketDashboardService dashboards) : ControllerBase
{
    [HttpGet("{cardSetId:guid}/dashboard")]
    public async Task<ActionResult<SetMarketDashboardDto>> Get(Guid cardSetId, CancellationToken ct)
    {
        var dashboard = await dashboards.GetDashboardAsync(cardSetId, ct);
        return dashboard == null ? NotFound() : dashboard;
    }

    [HttpGet("by-slug/{gameSlug}/{setSlug}/dashboard")]
    public async Task<ActionResult<SetMarketDashboardDto>> GetBySlug(string gameSlug, string setSlug, CancellationToken ct)
    {
        var dashboard = await dashboards.GetDashboardBySlugAsync(gameSlug, setSlug, ct);
        return dashboard == null ? NotFound() : dashboard;
    }
}
