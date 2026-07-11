using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Models;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsController(IBookingService bookings)
    {
        _bookings = bookings;
    }

    // Parent creates booking request
    [Authorize(Roles = "Parent")]
    [HttpPost]
    public async Task<IActionResult> Create(BookingCreateDto dto)
    {
        try
        {
            var id = await _bookings.CreateBookingRequestAsync(dto);
            return Ok(new { bookingId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Any authenticated user can view (simple version)
    [Authorize]
    [HttpGet("{bookingId:int}")]
    public async Task<IActionResult> Get(int bookingId)
    {
        try
        {
            var b = await _bookings.GetBookingAsync(bookingId);
            return Ok(b);
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

    // Sitter actions
    [Authorize(Roles = "BabySitter")]
    [HttpPost("{bookingId:int}/accept")]
    public async Task<IActionResult> Accept(int bookingId, BookingActionDto dto)
    {
        try
        {
            await _bookings.AcceptAsync(bookingId, dto);
            return Ok(new { accepted = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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

    [Authorize(Roles = "BabySitter")]
    [HttpPost("{bookingId:int}/reject")]
    public async Task<IActionResult> Reject(int bookingId, BookingActionDto dto)
    {
        try
        {
            await _bookings.RejectAsync(bookingId, dto);
            return Ok(new { rejected = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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

    [Authorize(Roles = "BabySitter")]
    [HttpPost("{bookingId:int}/confirm")]
    public async Task<IActionResult> Confirm(int bookingId, BookingActionDto dto)
    {
        try
        {
            await _bookings.ConfirmAsync(bookingId, dto);
            return Ok(new { confirmed = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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

    [Authorize(Roles = "BabySitter")]
    [HttpPost("{bookingId:int}/complete")]
    public async Task<IActionResult> Complete(int bookingId, BookingActionDto dto)
    {
        try
        {
            await _bookings.CompleteAsync(bookingId, dto);
            return Ok(new { completed = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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

    // Optional: conflict check endpoint
    [HttpGet("conflict")]
    public async Task<IActionResult> HasConflict(
        [FromQuery] int babySitterProfileId,
        [FromQuery] DateTime date,
        [FromQuery] TimeSpan startTime,
        [FromQuery] TimeSpan endTime)
    {
        try
        {
            var conflict = await _bookings.HasConflictAsync(babySitterProfileId, date, startTime, endTime);
            return Ok(new { conflict });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Parent cancels (or sitter cancel) - both allowed by service rule
    [Authorize]
    [HttpPost("{bookingId:int}/cancel")]
    public async Task<IActionResult> Cancel(int bookingId, BookingCancelDto dto)
    {
        try
        {
            await _bookings.CancelAsync(bookingId, dto);
            return Ok(new { cancelled = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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

    // Parent reschedules
    [Authorize(Roles = "Parent")]
    [HttpPost("{bookingId:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int bookingId, BookingRescheduleDto dto)
    {
        try
        {
            await _bookings.RescheduleAsync(bookingId, dto);
            return Ok(new { rescheduled = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
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