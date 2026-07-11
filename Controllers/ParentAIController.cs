using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;

namespace SmartBabySitter.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Parent")]
    public class ParentAIController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ParentAIController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string text = "")
        {
            text = (text ?? "").Trim().ToLower();

            var sitters = await _db.BabySitterProfiles
                .Include(x => x.User)
                .Where(x => x.IsApproved)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(text))
            {
                sitters = sitters.Where(x =>

                    (!string.IsNullOrEmpty(x.LocationText) &&
                     x.LocationText.ToLower().Contains(text))

                    ||

                    (!string.IsNullOrEmpty(x.User.FullName) &&
                     x.User.FullName.ToLower().Contains(text))

                ).ToList();
            }

            var result = sitters
                .OrderByDescending(x => x.ExperienceYears)
                .Select(x => new
                {
                    id = x.Id,
                    fullName = x.User.FullName,
                    location = x.LocationText,
                    experience = x.ExperienceYears,
                    hourlyRate = x.HourlyRate,
                    photoUrl = x.PhotoUrl
                });

            return Ok(result);
        }
    }
}
