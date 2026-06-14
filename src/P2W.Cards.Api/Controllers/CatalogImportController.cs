using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/catalog/import")]
public sealed class CatalogImportController(ICatalogImportService imports) : ControllerBase
{
    [HttpPost("preview")]
    public async Task<ActionResult<CatalogImportPreviewDto>> Preview(StartCatalogImportRequest request, CancellationToken ct)
    {
        try { return await imports.PreviewImportAsync(request, ct); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (HttpRequestException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("run")]
    public async Task<ActionResult<CatalogImportRunDto>> Run(StartCatalogImportRequest request, CancellationToken ct)
    {
        try { return await imports.StartImportAsync(request, ct); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (HttpRequestException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("runs")]
    public Task<IReadOnlyList<CatalogImportRunDto>> GetRuns([FromQuery] string? sourceName, [FromQuery] int take = 25, CancellationToken ct = default)
        => imports.GetImportRunsAsync(sourceName, take, ct);

    [HttpGet("runs/{importRunId:guid}")]
    public async Task<ActionResult<CatalogImportRunDetailDto>> GetRun(Guid importRunId, CancellationToken ct)
    {
        var run = await imports.GetImportRunAsync(importRunId, ct);
        return run == null ? NotFound() : run;
    }
}
