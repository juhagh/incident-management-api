using Application.DTOs;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class IncidentService : IIncidentQueries
{
    private readonly IApplicationDbContext _context;

    public IncidentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IncidentResponseDto?> GetByIdAsync(int id)
    {
        return await _context.Incidents
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new IncidentResponseDto
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                Severity = i.Severity,
                Status = i.Status,
                NetworkElementId = i.NetworkElementId,
                EngineerId = i.EngineerId,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                ClosedAt = i.ClosedAt,
                WaitingReason = i.WaitingReason,
                ResolutionSummary = i.ResolutionSummary,
                InvalidReason = i.InvalidReason,
                RowVersion = i.RowVersion
            })
            .FirstOrDefaultAsync();
    }
}