using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs;

public class CreateIncidentDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IncidentSeverity Severity { get; init; }
    public int NetworkElementId { get; init; }
}