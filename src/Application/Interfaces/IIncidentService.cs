using Application.DTOs;

namespace Application.Interfaces;

public interface IIncidentService
{
    Task<IncidentResponseDto?> GetByIdAsync(int id);
    Task<IncidentResponseDto> CreateAsync(CreateIncidentDto incidentDto);
    Task<(CommandResult, IncidentResponseDto?)> AssignEngineerAsync(int id, AssignEngineerDto dto, uint etag);
    Task<(CommandResult, IncidentResponseDto?)> StartProgressAsync(int id, uint etag);
    Task<(CommandResult, IncidentResponseDto?)> ResolveAsync(int id, ResolveIncidentDto dto, uint etag);
    Task<(CommandResult, IncidentResponseDto?)> MarkWaitingAsync(int id, MarkWaitingDto dto, uint etag);
    Task<(CommandResult, IncidentResponseDto?)> MarkInvalidAsync(int id, MarkInvalidDto dto, uint etag);
    Task<(CommandResult, IncidentResponseDto?)> CloseAsync(int id, uint etag);
}