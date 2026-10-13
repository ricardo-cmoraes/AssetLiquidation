using AssetLiquidation.Core;
using AssetLiquidation.Core.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssetLiquidation.Worker.Consumers;

public class OccurrenceConsumer : IConsumer<ProcessOccurrenceCommand>
{
    private readonly LiquidateDbContext _context;
    private readonly ILogger<OccurrenceConsumer> _logger;

    public OccurrenceConsumer (LiquidateDbContext context, ILogger<OccurrenceConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessOccurrenceCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Receiving command to Asset {AssetId}. CorrelationId: {CorrelationId}", 
            command.AssetId, command.CorrelationId);

        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId == command.AssetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset {AssetId} not found. Rejected Occurrence.", command.AssetId);
            return;
        }

        var occurrence = new Occurrence(asset.Id, command.Type, command.Amount);

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
                _logger.LogWarning("Not supported occurrence type: {Type}", command.Type);
                occurrence.MarkAsRejected($"Not supported occurrence type: {command.Type}");
                _context.Occurrences.Add(occurrence);
                await _context.SaveChangesAsync();
                return;
            }

            occurrence.MarkAsCompleted();
            _context.Occurrences.Add(occurrence);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Occurrence {OccurrenceId} processed successfully for Asset {AssetId}. New Balance: {Balance}", 
                occurrence.Id, asset.AssetId, asset.CurrentAmount);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            _logger.LogWarning(ex, "Domain validation failed for Asset {AssetId}: {Message}", command.AssetId, ex.Message);

            occurrence.MarkAsRejected(ex.Message);
            _context.Occurrences.Add(occurrence);
            await _context.SaveChangesAsync();
        }
    }
}