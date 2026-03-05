using API.Http.Etags;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentsController : ControllerBase
{

    private readonly IIncidentQueries _incidentService;

    public IncidentsController(IIncidentQueries incidentService)
    {
        _incidentService = incidentService;
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentResponseDto>> GetIncidentByIdAsync(int id)
    {
        var incident = await _incidentService.GetByIdAsync(id);
        if (incident is null)
            return NotFound();

        var etag = ETagHelper.CreateWeakETag(incident.RowVersion);
        
        if (ETagHelper.ShouldReturnNotModified(Request.Headers.IfNoneMatch, incident.RowVersion))
            return StatusCode(304);

        Response.Headers.ETag = etag;
        
        return Ok(incident);
    }
    
}