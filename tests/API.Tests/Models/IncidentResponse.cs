namespace API.Tests.Models;

public record IncidentResponse(
    int Id,
    string Title,
    string Description,
    string Severity,
    string Status,
    int NetworkElementId,
    int? EngineerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    string? WaitingReason,
    string? ResolutionSummary,
    string? InvalidReason
);