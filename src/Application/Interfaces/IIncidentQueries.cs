using Application.DTOs;

namespace Application.Interfaces;

public interface IIncidentQueries
{
    Task<IncidentResponseDto?> GetByIdAsync(int id);
    Task<IncidentResponseDto> CreateAsync(CreateIncidentDto incidentDto);
}