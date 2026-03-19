using Application.DTOs;

namespace Application.Interfaces;

public interface IIncidentService
{
    Task<IncidentResponseDto?> GetByIdAsync(int id);
    Task<IncidentResponseDto> CreateAsync(CreateIncidentDto incidentDto);
    Task<(CommandResult, IncidentResponseDto?)> AssignEngineerAsync(int id, AssignEngineerDto dto, uint etag);
}