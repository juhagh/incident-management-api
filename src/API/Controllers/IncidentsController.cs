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
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto createIncidentDto)
    {
        var incident = await _incidentService.CreateAsync(createIncidentDto);
        return CreatedAtAction("GetIncidentById", new { id = incident.Id }, incident);
    }
    
    [HttpPost("{id:int}/assign-engineer")]
    public async Task<IActionResult> AssignEngineer([FromBody] AssignEngineerDto assignEngineerDto, int id)
    {
        if (!TryGetIfMatchEtag(out uint etag, out var errorResult))
        { 
            return errorResult!;
        }
        
        var (result, incidentResponseDto) = await _incidentService.AssignEngineerAsync(id, assignEngineerDto, etag);

        return ToCommandActionResult(result, incidentResponseDto);
        
    }
    
    [HttpPost("{id:int}/start-progress")]
    public async Task<IActionResult> StartProgress(int id)
    {
        if (!TryGetIfMatchEtag(out uint etag, out var errorResult))
        { 
            return errorResult!;
        }
        
        var (result, incidentResponseDto) = await _incidentService.StartProgressAsync(id, etag);

        return ToCommandActionResult(result, incidentResponseDto);
    }
    
    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve([FromBody] ResolveIncidentDto resolveIncidentDto, int id)
    {
        if (!TryGetIfMatchEtag(out uint etag, out var errorResult))
        { 
            return errorResult!;
        }

        var (result, incidentResponseDto) = await _incidentService.ResolveAsync(id, resolveIncidentDto, etag);

        return ToCommandActionResult(result, incidentResponseDto);
    }
    
    [HttpPost("{id:int}/mark-waiting")]
    public async Task<IActionResult> MarkWaiting([FromBody] MarkWaitingDto markWaitingDto, int id)
    {

        if (!TryGetIfMatchEtag(out uint etag, out var errorResult))
        { 
            return errorResult!;
        }
        
        var (result, incidentResponseDto) = await _incidentService.MarkWaitingAsync(id, markWaitingDto, etag);
        return ToCommandActionResult(result, incidentResponseDto);
        
    }
    
    [HttpPost("{id:int}/mark-invalid")]
    public async Task<IActionResult> MarkInvalid([FromBody] MarkInvalidDto markInvalidDto, int id)
    {

        if (!TryGetIfMatchEtag(out uint etag, out var errorResult))
        { 
            return errorResult!;
        }
        
        var (result, incidentResponseDto) = await _incidentService.MarkInvalidAsync(id, markInvalidDto, etag);
        return ToCommandActionResult(result, incidentResponseDto);
        
    }
    
    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id)
    {

        if (!TryGetIfMatchEtag(out uint etag, out var errorResult))
        { 
            return errorResult!;
        }
        
        var (result, incidentResponseDto) = await _incidentService.CloseAsync(id, etag);
        return ToCommandActionResult(result, incidentResponseDto);
        
    }
    
    private IActionResult ToCommandActionResult(CommandResult result, IncidentResponseDto? incidentResponseDto)
    {
        if (result == CommandResult.Success && incidentResponseDto is null)
            return StatusCode(500);
        
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

    private bool TryGetIfMatchEtag(out uint etag, out IActionResult? errorResult)
    {
        etag = 0;
        errorResult = null;

        var header = Request.Headers["If-Match"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            errorResult = StatusCode(428);
            return false;
        }

        var parsed = ETagHelper.TryParseIfMatch(header);
        if (parsed is null)
        {
            errorResult = BadRequest();
            return false;
        }

        etag = parsed.Value;
        return true;
    }   
    
}