using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController(IDataProviderRegistry providers) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(providers.AllProviders.Select(p => new ProviderInfoDto
    {
        SourceName = p.SourceName,
        ProviderType = p.ProviderType.ToString(),
        IsEnabled = p.IsEnabled,
        Status = p.IsEnabled ? "Enabled" : "Disabled"
    }));

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        var checks = await Task.WhenAll(providers.AllProviders.Select(p => p.HealthCheckAsync(ct)));
        return Ok(checks);
    }
}
