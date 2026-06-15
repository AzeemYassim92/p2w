using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/products/{catalogProductId:guid}/chart")]
public sealed class MarketChartsController(IMarketChartService charts) : ControllerBase
{
    [HttpGet]
    public Task<MarketChartDto> Get(Guid catalogProductId, [FromQuery] MarketChartRequest request, CancellationToken ct)
        => charts.GetMarketChartAsync(catalogProductId, request, ct);
}
