using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Models;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    public AdminUsersController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    // GET: api/admin/users?query=abc
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? query = null)
    {
        var q = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            q = q.Where(u =>
                u.FullName.Contains(query) ||
                (u.Email ?? "").Contains(query) ||
                (u.PhoneNumber ?? "").Contains(query));
        }

        var users = await q
            .OrderBy(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.CreatedAt
            })
            .ToListAsync();

        // roles attach (simple loop)
        var result = new List<object>();
        foreach (var u in users)
        {
            var user = await _userManager.FindByIdAsync(u.Id.ToString());
            var roles = user == null ? new List<string>() : (await _userManager.GetRolesAsync(user)).ToList();
            result.Add(new { u.Id, u.FullName, u.Email, u.PhoneNumber, u.CreatedAt, roles });
        }

        return Ok(result);
    }
}