using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs;

public sealed class IncidentResponseDto
{
    public int Id { get; init; }

    public required string Title { get; init; }
    public required string Description { get; init; }

    public IncidentSeverity Severity { get; init; }
    public IncidentStatus Status { get; init; }

    public int NetworkElementId { get; init; }
    public int? EngineerId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }

    public string? WaitingReason { get; init; }
    public string? ResolutionSummary { get; init; }
    public string? InvalidReason { get; init; }
    
    [JsonIgnore]
    public uint RowVersion { get; init; }
}