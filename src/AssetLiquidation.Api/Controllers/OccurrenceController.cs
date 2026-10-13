using AssetLiquidation.Api.DTOs;
using AssetLiquidation.Core.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using MassTransit;
using System.Runtime.InteropServices;
using AssetLiquidation.Core;

namespace AssetLiquidation.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OccurrenceController : ControllerBase
{
    private readonly LiquidateDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    public OccurrenceController(LiquidateDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }
    
    [HttpPost]
    public async Task<IActionResult> ProcessOccurrence([FromBody] ProcessOccurrenceRequest request)
    {

        var command = new ProcessOccurrenceCommand
        {
            AssetId = request.AssetId,
            Type = request.Type,
            Amount = request.Amount
        };

        await _publishEndpoint.Publish(command);

        return Accepted(new
        {
            message = "Occurrence request successfully accepted for processing.",
            correlationId = command.CorrelationId,
            assetId = command.AssetId,
            status = "Queued",
            queueAt = command.CreatedAt
        });
    }   

    [HttpGet("occurrence/{assetId}")]
    public async Task<IActionResult> GetOccurrencesByAsset(string assetId)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId == assetId);

        if (asset == null)
            return NotFound(new { message = $"Asset {assetId} not found." });

        var occurrences = await _context.Occurrences
            .Where(o => o.AssetId == asset.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OccurrenceResponse(
                o.Id,
                asset.AssetId,
                o.Type,
                o.Amount,
                o.Status,
                o.CreatedAt,
                o.Reason
            ))
            .ToListAsync();

        return Ok(occurrences);
    }
}