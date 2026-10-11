using AssetLiquidation.Api.DTOs;
using AssetLiquidation.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace AssetLiquidation.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OccurrenceController : ControllerBase
{
    private readonly LiquidateDbContext _context;

    public OccurrenceController(LiquidateDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessOccurrence([FromBody] ProcessOccurrenceRequest request)
    {
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId == request.AssetId);

        if (asset == null)
            return NotFound(new { message = $"Asset {request.AssetId} not found." });
        
        var occurrence = new Occurrence(asset.Id, request.Type, request.Amount);

        try
        {
            if (occurrence.Type == OccurrenceType.Payment)
            {
                asset.ProcessPayment(occurrence.Amount);
            }
            else if (occurrence.Type == OccurrenceType.Refund)
            {
                asset.ProcessRefund(occurrence.Amount);
            }
            else if (occurrence.Type == OccurrenceType.Cancellation)
            {
                asset.ProcessCancellation();
            }
            else
            {
                return BadRequest(new { message = "Not suppported occurrence type"});
            }

            occurrence.MarkAsCompleted();
            _context.Occurrences.Add(occurrence);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Occurrence successfully processed.",
                occurrenceId = occurrence.Id,
                assetId = asset.AssetId,
                newCurrentAmount = asset.CurrentAmount,
                assetStatus = asset.Status.ToString()
            });
        }
        catch (ArgumentException ex)
        {
            occurrence.MarkAsRejected(ex.Message);
            _context.Occurrences.Add(occurrence);
            await _context.SaveChangesAsync();

            return BadRequest(new {message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            occurrence.MarkAsRejected(ex.Message);
            _context.Occurrences.Add(occurrence);
            await _context.SaveChangesAsync();

            return UnprocessableEntity(new {message = ex.Message });
        }
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