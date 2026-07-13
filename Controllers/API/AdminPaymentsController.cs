using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminPaymentsController(ApplicationDbContext db) => _db = db;

    [HttpPost("{paymentId:int}/approve")]
    public async Task<IActionResult> Approve(int paymentId)
    {
        var payment = await _db.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.ParentUser)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BabySitterProfile)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId);


        if (payment == null)
            return NotFound("Payment not found");


        // 1. Payment Update
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;


        // 2. Booking Update
        payment.Booking.Status = BookingStatus.Confirmed;



        // 3. Notification Create (এখানে বসবে)
        var parentNotification = new Notification
        {
            ReceiverUserId = payment.Booking.ParentUser.Id,
            Type = NotificationType.InApp,
            Title = "Payment Approved",
            Message = "Your payment has been approved. Your booking is confirmed.",
            IsSent = true,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };


        var sitterNotification = new Notification
        {
            ReceiverUserId = payment.Booking.BabySitterProfile.User.Id,
            Type = NotificationType.InApp,
            Title = "Booking Confirmed",
            Message = "Payment completed. Your babysitting booking is confirmed.",
            IsSent = true,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };


        _db.Notifications.Add(parentNotification);
        _db.Notifications.Add(sitterNotification);



        // 4. Save all changes
        await _db.SaveChangesAsync();


        return Ok(new
        {
            message = "Payment approved successfully"
        });
    }

    [HttpPost("{paymentId:int}/reject")]
    public async Task<IActionResult> Reject(int paymentId)
    {
        var payment = await _db.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.ParentUser)
            .FirstOrDefaultAsync(p => p.Id == paymentId);


        if (payment == null)
            return NotFound("Payment not found");


        // Payment Update
        payment.Status = PaymentStatus.Rejected;


        // Booking Update
        payment.Booking.Status = BookingStatus.Cancelled;


        // Parent Notification
        var notification = new Notification
        {
            ReceiverUserId = payment.Booking.ParentUser.Id,
            Type = NotificationType.InApp,
            Title = "Payment Rejected",
            Message = "Your payment has been rejected by admin.",
            IsSent = true,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };


        _db.Notifications.Add(notification);


        await _db.SaveChangesAsync();


        return Ok(new
        {
            message = "Payment rejected successfully"
        });
    }

    // GET: api/admin/payments?status=Paid
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaymentStatus? status = null)
    {
        var q = _db.Payments
    .AsNoTracking()
    .Include(p => p.Booking)
        .ThenInclude(b => b.ParentUser)
    .Include(p => p.Booking)
        .ThenInclude(b => b.BabySitterProfile)
            .ThenInclude(s => s.User)
    .AsQueryable();

        if (status.HasValue)
            q = q.Where(p => p.Status == status.Value);

        var items = await q
            .OrderByDescending(p => p.CreatedAt)
           .Select(p => new
           {
               p.Id,
               p.BookingId,

               ParentName = p.Booking.ParentUser.FullName,

               SitterName = p.Booking.BabySitterProfile.User.FullName,

               p.Amount,
               p.Method,
               p.TransactionId,

               p.Status,
               StatusName = p.Status.ToString(),

               p.CreatedAt,
               p.PaidAt
           })

            .ToListAsync();

        return Ok(items);
    }
}