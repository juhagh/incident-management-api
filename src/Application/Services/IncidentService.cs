using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class IncidentService : IIncidentService
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

    public async Task<IncidentResponseDto> CreateAsync(CreateIncidentDto createIncidentDto)
    {
        var incident = Incident.Create(createIncidentDto.Title, createIncidentDto.Description, createIncidentDto.Severity,
            createIncidentDto.NetworkElementId);
        
        _context.AddEntity(incident);
        await _context.SaveChangesAsync();

        return MapToDto(incident);
    }

    public async Task<(CommandResult, IncidentResponseDto?)> AssignEngineerAsync(int id, AssignEngineerDto assignEngineerDto, uint etag)
    {
        return await ExecuteIncidentCommandAsync(id, etag, incident => incident.AssignEngineer(assignEngineerDto.EngineerId));
    }

    public async Task<(CommandResult, IncidentResponseDto?)> StartProgressAsync(int id, uint etag)
    {
        return await ExecuteIncidentCommandAsync(id, etag, incident => incident.StartProgress());
    }

    public async Task<(CommandResult, IncidentResponseDto?)> ResolveAsync(int id, ResolveIncidentDto resolveIncidentDto, uint etag)
    {
        return await ExecuteIncidentCommandAsync(id, etag,
            incident => incident.Resolve(resolveIncidentDto.ResolutionSummary));
    }

    public async Task<(CommandResult, IncidentResponseDto?)> MarkWaitingAsync(int id, MarkWaitingDto markWaitingDto, uint etag)
    {
        return await ExecuteIncidentCommandAsync(id, etag, incident => incident.MarkWaiting(markWaitingDto.Reason));
    }

    public async Task<(CommandResult, IncidentResponseDto?)> MarkInvalidAsync(int id, MarkInvalidDto markInvalidDto, uint etag)
    {
        return await ExecuteIncidentCommandAsync(id, etag, incident => incident.MarkInvalid(markInvalidDto.Reason));
    }

    public async Task<(CommandResult, IncidentResponseDto?)> CloseAsync(int id, uint etag)
    {
        return await ExecuteIncidentCommandAsync(id, etag, incident => incident.Close());
    }

    private IncidentResponseDto MapToDto(Incident incident)
    {
        return new IncidentResponseDto
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Severity = incident.Severity,
            Status = incident.Status,
            NetworkElementId = incident.NetworkElementId,
            EngineerId = incident.EngineerId,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt,
            ClosedAt = incident.ClosedAt,
            WaitingReason = incident.WaitingReason,
            ResolutionSummary = incident.ResolutionSummary,
            InvalidReason = incident.InvalidReason,
            RowVersion = incident.RowVersion
        };
    }

    private async Task<(CommandResult, IncidentResponseDto?)> ExecuteIncidentCommandAsync(
        int id,
        uint etag,
        Action<Incident> applyDomainAction)
    {
        var incident = await _context.Incidents
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident is null)
            return (CommandResult.NotFound, null);

        if (etag != incident.RowVersion)
            return (CommandResult.ConcurrencyConflict, null);
                
        try
        {
            applyDomainAction(incident);
        }
        catch (InvalidOperationException)
        {
            return (CommandResult.InvalidStateTransition, null);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (CommandResult.ConcurrencyConflict, null);
        }

        return (CommandResult.Success, MapToDto(incident));
    }
}