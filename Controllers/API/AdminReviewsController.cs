using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBabySitter.Services;
using SmartBabySitter.Services.DTOs;
using System.Security.Claims;

namespace SmartBabySitter.Controllers.API;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;

    public AdminReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    // GET: /api/admin/reviews?approved=false&hidden=false&page=1&pageSize=20&query=rahim
    [HttpGet]
    public async Task<IActionResult> List(bool? approved, bool? hidden, int page = 1, int pageSize = 20, string? query = null)
        => Ok(await _reviews.AdminListAsync(approved, hidden, page, pageSize, query));

    // POST: /api/admin/reviews/{id}/decision
    [HttpPost("{id:int}/decision")]
    public async Task<IActionResult> Decision(int id, [FromBody] ReviewAdminDecisionDto dto)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _reviews.AdminDecisionAsync(id, dto, adminId);
        return Ok(new { updated = true });
    }
}