using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
        => Ok(await _admin.GetDashboardAsync());

    // GET: /api/admin/revenue-monthly?year=2026
    [HttpGet("revenue-monthly")]
    public async Task<IActionResult> RevenueMonthly([FromQuery] int year)
        => Ok(await _admin.GetRevenueMonthlyAsync(year));

    // GET: /api/admin/calendar?from=2026-02-01&to=2026-02-28
    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar([FromQuery] DateTime from, [FromQuery] DateTime to)
        => Ok(await _admin.GetCalendarAsync(from, to));
}