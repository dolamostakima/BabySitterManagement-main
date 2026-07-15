using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendance;

    public AttendanceController(IAttendanceService attendance)
    {
        _attendance = attendance;
    }

    [Authorize(Roles = "BabySitter")]
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(AttendanceCheckInDto dto)
    {
        try
        {
            await _attendance.CheckInAsync(dto);
            return Ok(new { checkedIn = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "BabySitter")]
    [HttpPost("{bookingId:int}/check-out")]
    public async Task<IActionResult> CheckOut(int bookingId)
    {
        try
        {
            await _attendance.CheckOutAsync(bookingId);
            return Ok(new { checkedOut = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("booking/{bookingId:int}")]
    public async Task<IActionResult> GetByBooking(int bookingId)
    {
        var attendance = await _attendance.GetByBookingAsync(bookingId);

        if (attendance == null)
            return NotFound();

        return Ok(attendance);
    }

    [Authorize(Roles = "BabySitter")]
    [HttpGet("my-history")]
    public async Task<IActionResult> MyHistory()
    {
        var data = await _attendance.GetMyAttendanceAsync();
        return Ok(data);
    }
    [Authorize(Roles = "Parent")]
    [HttpGet("parent-history")]
    public async Task<IActionResult> ParentHistory()
    {
        var data = await _attendance.GetParentAttendanceAsync();
        return Ok(data);
    }


}