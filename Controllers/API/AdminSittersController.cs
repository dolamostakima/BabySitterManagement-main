using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/admin/sitters")]
[Authorize(Roles = "Admin")]
public class AdminSittersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminSittersController(ApplicationDbContext db) => _db = db;

    // GET: api/admin/sitters?approved=true/false&query=abc
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? approved = null, [FromQuery] string? query = null)
    {
        var q = _db.BabySitterProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .AsQueryable();

        if (approved.HasValue)
            q = q.Where(s => s.IsApproved == approved.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            q = q.Where(s =>
                s.User.FullName.Contains(query) ||
                (s.User.Email ?? "").Contains(query) ||
                (s.LocationText ?? "").Contains(query));
        }

        var items = await q
            .OrderBy(s => s.IsApproved)
            .ThenByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.UserId,
                FullName = s.User.FullName,
                Email = s.User.Email,
                Phone = s.User.PhoneNumber,
                s.HourlyRate,
                s.ExperienceYears,
                s.LocationText,
                s.IsApproved,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}