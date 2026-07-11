using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;
using System.Security.Claims;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = "Admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookingService;

    public AdminBookingsController(ApplicationDbContext db, IBookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    // ✅ GET: /api/admin/bookings?status=Pending&from=2026-02-01&to=2026-02-28&page=1&pageSize=20&query=rahim
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] BookingStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // (তুমি চাইলে এই অংশটা BookingService এও নিতে পারো, আপাতত keep)
        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var q = _db.Bookings.AsNoTracking()
            .Include(b => b.ParentUser)
            .Include(b => b.BabySitterProfile).ThenInclude(s => s.User)
            .AsQueryable();

        if (status.HasValue) q = q.Where(b => b.Status == status.Value);
        if (from.HasValue) q = q.Where(b => b.BookingDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(b => b.BookingDate <= to.Value.Date);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            q = q.Where(b =>
                b.ParentUser.FullName.Contains(query) ||
                (b.ParentUser.Email ?? "").Contains(query) ||
                b.BabySitterProfile.User.FullName.Contains(query) ||
                (b.BabySitterProfile.User.Email ?? "").Contains(query) ||
                b.ServiceAddressText.Contains(query)
            );
        }

        q = q.OrderByDescending(b => b.CreatedAt);

        var total = await q.CountAsync();

        var items = await q.Skip((p - 1) * size).Take(size)
            .Select(b => new
            {
                b.Id,
                status = b.Status.ToString(), // ✅ Status: "Accepted" (2 না)
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                b.TotalAmount,
                b.CreatedAt,
                b.ServiceAddressText,
                Parent = new { b.ParentUserId, b.ParentUser.FullName, b.ParentUser.Email, b.ParentUser.PhoneNumber },
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

    // ✅ GET: /api/admin/bookings/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var b = await _db.Bookings.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                status = x.Status.ToString(),
                x.BookingDate,
                x.StartTime,
                x.EndTime,
                x.TotalAmount,
                x.CreatedAt,
                x.ServiceAddressText,

                ParentUser = new
                {
                    x.ParentUserId,
                    x.ParentUser.FullName,
                    x.ParentUser.Email,
                    x.ParentUser.PhoneNumber
                },

                BabySitterProfile = new
                {
                    x.BabySitterProfileId,
                    x.BabySitterProfile.HourlyRate,
                    x.BabySitterProfile.ExperienceYears,
                    x.BabySitterProfile.LocationText,
                    x.BabySitterProfile.IsApproved,
                    User = new
                    {
                        x.BabySitterProfile.User.FullName,
                        x.BabySitterProfile.User.Email,
                        x.BabySitterProfile.User.PhoneNumber
                    }
                },

                Payment = x.Payment == null ? null : new
                {
                    x.Payment.Id,
                    x.Payment.Amount,
                    x.Payment.Method,
                    x.Payment.TransactionId,
                    status = x.Payment.Status.ToString(),
                    x.Payment.CreatedAt,
                    x.Payment.PaidAt
                },

                Review = x.Review == null ? null : new
                {
                    x.Review.Id,
                    x.Review.Rating,
                    x.Review.Comment,
                    x.Review.IsApproved,
                    x.Review.CreatedAt,
                    x.Review.SitterReply,
                    x.Review.SitterReplyAt
                },

                Attendance = x.Attendance == null ? null : new
                {
                    x.Attendance.Id,
                    x.Attendance.CheckInTime,
                    x.Attendance.CheckOutTime,
                    x.Attendance.LocationText,
                    x.Attendance.Latitude,
                    x.Attendance.Longitude
                },

                StatusHistory = x.StatusHistory
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new
                    {
                        h.Id,
                        fromStatus = h.FromStatus.ToString(),
                        toStatus = h.ToStatus.ToString(),
                        h.Note,
                        h.ChangedAt,
                        ChangedBy = new
                        {
                            h.ChangedByUserId,
                            h.ChangedByUser.FullName,
                            h.ChangedByUser.Email
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (b == null) return NotFound("Booking not found.");
        return Ok(b);
    }

    // ✅ POST: /api/admin/bookings/{id}/status
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _bookingService.ChangeBookingStatusAsync(id, dto.ToStatus, dto.Note, adminId);
        return Ok(new { changed = true });
    }

    // ✅ POST: /api/admin/bookings/{id}/payment/mark-paid
    [HttpPost("{id:int}/payment/mark-paid")]
    public async Task<IActionResult> MarkPaid(int id, [FromBody] MarkPaidDto dto)
    {
        await _bookingService.MarkPaymentPaidAsync(id, dto.Method, dto.TransactionId);
        return Ok(new { success = true });
    }

    // ✅ POST: /api/admin/bookings/{id}/attendance/checkin
    [HttpPost("{id:int}/attendance/checkin")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var res = await _bookingService.CheckInAsync(id);
        return Ok(res);
    }

    // ✅ POST: /api/admin/bookings/{id}/attendance/checkout
    [HttpPost("{id:int}/attendance/checkout")]
    public async Task<IActionResult> CheckOut(int id)
    {
        await _bookingService.CheckOutAsync(id);
        return Ok(new { checkedOut = true });
    }
}