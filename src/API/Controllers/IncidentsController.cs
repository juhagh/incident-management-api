using API.Http.Etags;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentsController : ControllerBase
{

    private readonly IIncidentQueries _incidentService;

    public IncidentsController(IIncidentQueries incidentService)
    {
        _incidentService = incidentService;
    }
    
    [AllowAnonymous]
    [HttpGet("{id:int}", Name = "GetIncidentById")]
    public async Task<ActionResult<IncidentResponseDto>> GetIncidentByIdAsync(int id)
    {
        var incident = await _incidentService.GetByIdAsync(id);

        if (incident is null)
            return NotFound();

        return this.ConditionalOk(incident, incident.RowVersion);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto incidentDto)
    {
        var incident = await _incidentService.CreateAsync(incidentDto);
        return CreatedAtAction("GetIncidentById", new { id = incident.Id }, incident);
    }
}