using System.Dynamic;
using AssetLiquidation.Core.Migrations;

namespace AssetLiquidation.Core.Contracts;

public record ProcessOccurrenceCommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public string AssetId { get; init; } = string.Empty;
    public OccurrenceType Type { get; init; }
    public decimal Amount { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}