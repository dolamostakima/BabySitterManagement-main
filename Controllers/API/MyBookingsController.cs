using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/my-bookings")]
[Authorize]
public class MyBookingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;

    public MyBookingsController(ApplicationDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    // GET: api/my-bookings/parent
    [Authorize(Roles = "Parent")]
    [HttpGet("parent")]
    public async Task<IActionResult> ParentBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BookingStatus? status = null)
    {
        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var q = _db.Bookings
            .AsNoTracking()
            .Include(b => b.BabySitterProfile).ThenInclude(s => s.User)
            .Where(b => b.ParentUserId == _me.UserId);

        if (status.HasValue)
            q = q.Where(b => b.Status == status.Value);

        q = q.OrderByDescending(b => b.CreatedAt);

        var total = await q.CountAsync();

        var items = await q
            .Skip((p - 1) * size)
            .Take(size)
            .Select(b => new
            {
                b.Id,
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                b.Status,
                StatusName = b.Status.ToString(),
                b.TotalAmount,
                b.CreatedAt,
                b.ServiceAddressText,
                Sitter = new
                {
                    b.BabySitterProfileId,
                    b.BabySitterProfile.UserId,
                    b.BabySitterProfile.User.FullName,
                    b.BabySitterProfile.User.Email,
                    b.BabySitterProfile.User.PhoneNumber,
                    b.BabySitterProfile.HourlyRate,
                    b.BabySitterProfile.ExperienceYears,
                    b.BabySitterProfile.LocationText,
                    b.BabySitterProfile.IsApproved
                }
            })
            .ToListAsync();

        return Ok(new { total, page = p, pageSize = size, items });
    }

    [Authorize(Roles = "Parent")]
    [HttpGet("parent/payment")]
    public async Task<IActionResult> ParentPaymentBookings()
    {
        var items = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.BabySitterProfile)
            .ThenInclude(s => s.User)
            .Where(b =>
                b.ParentUserId == _me.UserId &&
                (b.Status == BookingStatus.Accepted ||
                 b.Status == BookingStatus.Confirmed))
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                b.Status,
                StatusName = b.Status.ToString(),
                b.TotalAmount,
                Sitter = new
                {
                    b.BabySitterProfile.User.FullName
                }
            })
            .ToListAsync();

        return Ok(new { items });
    }

    // GET: api/my-bookings/sitter
    [Authorize(Roles = "BabySitter")]
    [HttpGet("sitter")]
    public async Task<IActionResult> SitterBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BookingStatus? status = null)
    {
        var sitter = await _db.BabySitterProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _me.UserId);

        if (sitter == null) return BadRequest("Sitter profile not found. Create profile first.");

        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var q = _db.Bookings
            .AsNoTracking()
            .Include(b => b.ParentUser)
            .Where(b => b.BabySitterProfileId == sitter.Id);

        if (status.HasValue)
            q = q.Where(b => b.Status == status.Value);

        q = q.OrderByDescending(b => b.CreatedAt);

        var total = await q.CountAsync();

        var items = await q
            .Skip((p - 1) * size)
            .Take(size)
            .Select(b => new
            {
                b.Id,
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                b.Status,
                StatusName = b.Status.ToString(),
               
                b.TotalAmount,
                b.CreatedAt,
                b.ServiceAddressText,
                Parent = new
                {
                    b.ParentUserId,
                    b.ParentUser.FullName,
                    b.ParentUser.Email,
                    b.ParentUser.PhoneNumber
                }
            })
            .ToListAsync();

        return Ok(new { total, page = p, pageSize = size, items });
    }
}