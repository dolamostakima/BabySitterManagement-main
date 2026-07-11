using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "BabySitter")]
public class AvailabilitiesController : ControllerBase
{
    private readonly IAvailabilityService _availability;

    public AvailabilitiesController(IAvailabilityService availability)
    {
        _availability = availability;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyAvailabilities()
    {
        var list = await _availability.GetMyAvailabilitiesAsync();
        var dto = list.Select(a => new AvailabilityDto(
     a.Id,
     a.Day,
     a.Date,
     a.EndDate,
     a.StartTime,
     a.EndTime,
     a.IsAvailable
 ));
        return Ok(dto);
    }

    [HttpPost("me")]
    public async Task<IActionResult> AddMyAvailability([FromBody] AvailabilityCreateDto dto)
    {
        try
        {
            var id = await _availability.AddMyAvailabilityAsync(dto);
            return Ok(new { availabilityId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("me/{availabilityId:int}")]
    public async Task<IActionResult> DeleteMyAvailability(int availabilityId)
    {
        try
        {
            await _availability.RemoveMyAvailabilityAsync(availabilityId);
            return Ok(new { deleted = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}