using Microsoft.AspNetCore.Mvc;
using P2W.Cards.Application.DTOs;
using P2W.Cards.Application.Interfaces;

namespace P2W.Cards.Api.Controllers;

[ApiController]
[Route("api/catalog/mappings")]
public sealed class CatalogMappingController(IMappingReviewService mappings) : ControllerBase
{
    [HttpGet("review")]
    public Task<IReadOnlyList<MappingReviewDto>> GetReview([FromQuery] string? status = "NeedsReview", [FromQuery] int take = 50, CancellationToken ct = default)
        => mappings.GetMappingsForReviewAsync(status, take, ct);

    [HttpPatch("{mappingId:guid}/approve")]
    public async Task<ActionResult<MappingReviewDto>> Approve(Guid mappingId, CancellationToken ct)
    {
        var mapping = await mappings.ApproveAsync(mappingId, ct);
        return mapping == null ? NotFound() : mapping;
    }

    [HttpPatch("{mappingId:guid}/reject")]
    public async Task<ActionResult<MappingReviewDto>> Reject(Guid mappingId, CancellationToken ct)
    {
        var mapping = await mappings.RejectAsync(mappingId, ct);
        return mapping == null ? NotFound() : mapping;
    }

    [HttpPatch("{mappingId:guid}/notes")]
    public async Task<ActionResult<MappingReviewDto>> SaveNotes(Guid mappingId, UpdateMappingNotesRequest request, CancellationToken ct)
    {
        var mapping = await mappings.SaveNotesAsync(mappingId, request.Notes, ct);
        return mapping == null ? NotFound() : mapping;
    }
}
