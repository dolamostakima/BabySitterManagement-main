using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Parent")]
public class FavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;

    public FavoritesController(ApplicationDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    // GET: api/favorites/me
    [HttpGet("me")]
    public async Task<IActionResult> MyFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var q = _db.Favorites
            .AsNoTracking()
            .Where(f => f.ParentUserId == _me.UserId)
            .OrderByDescending(f => f.CreatedAt);

        var total = await q.CountAsync();

        var items = await q
            .Skip((p - 1) * size)
            .Take(size)
            .Join(_db.BabySitterProfiles.Include(s => s.User),
                f => f.BabySitterProfileId,
                s => s.Id,
                (f, s) => new
                {
                    f.Id,
                    f.CreatedAt,
                    BabySitterProfileId = s.Id,
                    s.UserId,
                    FullName = s.User.FullName,
                    Email = s.User.Email,
                    Phone = s.User.PhoneNumber,
                    s.HourlyRate,
                    s.ExperienceYears,
                    s.LocationText,
                    s.IsApproved,
                    AvgRating = _db.Reviews
                        .Where(r => r.BabySitterProfileId == s.Id)
                        .Select(r => (double?)r.Rating)
                        .Average() ?? 0,
                    ReviewCount = _db.Reviews.Count(r => r.BabySitterProfileId == s.Id)
                })
            .ToListAsync();

        return Ok(new { total, page = p, pageSize = size, items });
    }

    // POST: api/favorites/{babySitterProfileId}
    [HttpPost("{babySitterProfileId:int}")]
    public async Task<IActionResult> Add(int babySitterProfileId)
    {
        var sitterExists = await _db.BabySitterProfiles.AnyAsync(s => s.Id == babySitterProfileId);
        if (!sitterExists) return NotFound("Sitter profile not found.");

        var exists = await _db.Favorites.AnyAsync(f =>
            f.ParentUserId == _me.UserId && f.BabySitterProfileId == babySitterProfileId);

        if (exists) return Ok(new { added = false, message = "Already in favorites." });

        var fav = new Favorite
        {
            ParentUserId = _me.UserId,
            BabySitterProfileId = babySitterProfileId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Favorites.Add(fav);
        await _db.SaveChangesAsync();

        return Ok(new { added = true, favoriteId = fav.Id });
    }

    // DELETE: api/favorites/{babySitterProfileId}
    [HttpDelete("{babySitterProfileId:int}")]
    public async Task<IActionResult> Remove(int babySitterProfileId)
    {
        var fav = await _db.Favorites.FirstOrDefaultAsync(f =>
            f.ParentUserId == _me.UserId && f.BabySitterProfileId == babySitterProfileId);

        if (fav == null) return NotFound("Favorite not found.");

        _db.Favorites.Remove(fav);
        await _db.SaveChangesAsync();

        return Ok(new { deleted = true });
    }
}