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

    // GET: api/admin/payments?status=Paid
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaymentStatus? status = null)
    {
        var q = _db.Payments
            .AsNoTracking()
            .Include(p => p.Booking)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(p => p.Status == status.Value);

        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.BookingId,
                p.Amount,
                p.Method,
                p.TransactionId,
                p.Status,
                p.CreatedAt,
                p.PaidAt
            })
            .ToListAsync();

        return Ok(items);
    }
}