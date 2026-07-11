using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
public class SittersController : ControllerBase
{
    private readonly ISitterService _sitters;

    public SittersController(ISitterService sitters)
    {
        _sitters = sitters;
    }

    // BabySitter নিজের profile create/update
    [Authorize(Roles = "BabySitter")]
    [HttpPost("me/profile")]
    public async Task<IActionResult> UpsertMyProfile(SitterProfileUpsertDto dto)
    {
        try
        {
            var id = await _sitters.UpsertMySitterProfileAsync(dto);
            return Ok(new { babySitterProfileId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "BabySitter")]
    [HttpGet("me/profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var s = await _sitters.GetMyProfileAsync();   // নতুন service method
        if (s == null) return Ok(null);
        return Ok(s);
    }

    // Public: sitter details
    [HttpGet("{babySitterProfileId:int}")]
    public async Task<IActionResult> Get(int babySitterProfileId)
    {
        var s = await _sitters.GetSitterAsync(babySitterProfileId);
        if (s == null) return NotFound();
        return Ok(s);
    }

    // Public: search/filter/recommendation
    [HttpPost("search")]
    public async Task<IActionResult> Search(SitterSearchQueryDto q)
    {
        try
        {
            var result = await _sitters.SearchAsync(q);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Admin approve/reject sitter
    [Authorize(Roles = "Admin")]
    [HttpPut("{babySitterProfileId:int}/approve")]
    public async Task<IActionResult> Approve(int babySitterProfileId, [FromQuery] bool approve = true)
    {
        try
        {
            await _sitters.ApproveSitterAsync(babySitterProfileId, approve);
            return Ok(new { approved = approve });
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