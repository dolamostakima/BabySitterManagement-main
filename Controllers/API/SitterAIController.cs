using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Services;
using System.Security.Claims;

namespace SmartBabySitter.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "BabySitter")]
    public class SitterAIController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public SitterAIController(
            ApplicationDbContext db,
            ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions()
        {
            var profile = await _db.BabySitterProfiles
                .Include(x => x.Availabilities)
                .FirstOrDefaultAsync(x => x.UserId == _currentUser.UserId);

            if (profile == null)
                return NotFound("Profile not found.");

            var suggestions = new List<string>();

            // ===== Profile Photo =====
            if (string.IsNullOrWhiteSpace(profile.PhotoUrl))
            {
                suggestions.Add("📷 Upload a professional profile picture to increase trust.");
            }

            // ===== Skills =====
            if (string.IsNullOrWhiteSpace(profile.SkillsText))
            {
                suggestions.Add("🧸 Add skills like First Aid, Infant Care and Newborn Care.");
            }

            // ===== Experience =====
            if (profile.ExperienceYears < 2)
            {
                suggestions.Add("⭐ Gain more babysitting experience to attract more parents.");
            }

            // ===== Hourly Rate =====
            if (profile.HourlyRate < 100)
            {
                suggestions.Add("💰 Consider increasing your hourly rate based on your experience.");
            }

            // ===== Location =====
            if (string.IsNullOrWhiteSpace(profile.LocationText))
            {
                suggestions.Add("📍 Add your service location so parents can find you easily.");
            }

            // ===== Availability =====
            if (!profile.Availabilities.Any())
            {
                suggestions.Add("📅 Update your availability to receive booking requests.");
            }

            // ===== Perfect Profile =====
            if (!suggestions.Any())
            {
                suggestions.Add("🎉 Excellent! Your profile is complete.");
                suggestions.Add("🚀 Keep your availability updated.");
                suggestions.Add("⭐ Ask parents for reviews to improve your ranking.");
            }

            return Ok(suggestions);
        }
    }
}