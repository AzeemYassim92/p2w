using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/market/providers/health")]
public sealed class MarketProviderHealthController(IMarketProviderHealthService health) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProviderHealthDto>> Get(CancellationToken ct)
        => health.GetHealthAsync(ct);
}
