using API.Http.Etags;
using Application;
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

    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
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
    
    [HttpPost("{id:int}/assign-engineer")]
    public async Task<IActionResult> AssignEngineer([FromBody] AssignEngineerDto engineerDto, int id)
    {
        var header = Request.Headers["If-Match"].ToString();
        if (string.IsNullOrWhiteSpace(header))
            return StatusCode(428);
        
        var etag = ETagHelper.TryParseIfMatch(header);
        if (etag is null)
            return BadRequest();    
        
        var (result, incidentResponseDto) = await _incidentService.AssignEngineerAsync(id, engineerDto, etag.Value);

        if (result == CommandResult.Success)
        {
            Response.Headers.ETag = ETagHelper.CreateWeakETag(incidentResponseDto!.RowVersion);
            Response.Headers.CacheControl = "private, max-age=0";
            return Ok(incidentResponseDto);
        }

        return result switch
        {
            CommandResult.NotFound => NotFound(),
            CommandResult.ConcurrencyConflict => StatusCode(412),
            CommandResult.InvalidStateTransition => Conflict(),
            _ => StatusCode(500)
        };

    }
}