using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Services;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;

    public NotificationsController(ApplicationDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    [HttpGet("me")]
    public async Task<IActionResult> MyNotifications([FromQuery] int take = 50)
    {
        var size = Math.Clamp(take, 1, 200);

        var list = await _db.Notifications
    .AsNoTracking()
    .Include(n => n.ReceiverUser)
    .Where(n => n.ReceiverUserId == _me.UserId)
    .OrderByDescending(n => n.CreatedAt)
    .Take(size)
    .Select(n => new
    {
        n.Id,
        ReceiverName = n.ReceiverUser.FullName,
        n.Title,
        n.Message,
        n.Type,
        n.IsSent,
        n.CreatedAt
    })
    .ToListAsync();

        return Ok(list);
    }
}