using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Services;

namespace SmartBabySitter.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "BabySitter")]
    public class SitterDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public SitterDashboardController(
            ApplicationDbContext db,
            ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var profile = await _db.BabySitterProfiles
                .Include(x => x.User)
                .Include(x => x.Availabilities)
                .FirstOrDefaultAsync(x => x.UserId == _currentUser.UserId);

            if (profile == null)
                return NotFound("Baby sitter profile not found.");

            // Total Bookings
            var bookingCount = await _db.Bookings
                .CountAsync(x => x.BabySitterProfileId == profile.Id);

            // Total Reviews
            var reviewCount = await _db.Reviews
                .CountAsync(x => x.BabySitterProfileId == profile.Id);

            // Average Rating
            var averageRating = await _db.Reviews
                .Where(x => x.BabySitterProfileId == profile.Id)
                .Select(x => (double?)x.Rating)
                .AverageAsync() ?? 0;

            // Notifications
            var notificationCount = await _db.Notifications
                .CountAsync(x => x.ReceiverUserId == profile.UserId);

            var notifications = await _db.Notifications
    .Where(x => x.ReceiverUserId == profile.UserId)
    .OrderByDescending(x => x.CreatedAt)
    .Take(5)
    .Select(x => new
    {
        x.Title,
        x.Message,
        x.CreatedAt
    })
    .ToListAsync();


            var result = new
            {
                fullName = profile.User.FullName,
                photoUrl = profile.PhotoUrl,
                experienceYears = profile.ExperienceYears,
                hourlyRate = profile.HourlyRate,
                location = profile.LocationText,
                skills = profile.SkillsText,

                availabilityCount = profile.Availabilities.Count,

                rating = Math.Round(averageRating, 1),
                verified = profile.IsApproved,

                bookingCount = bookingCount,
                reviewCount = reviewCount,
                notificationCount = notificationCount,
                notifications = notifications
            };

            return Ok(result);
        }
    }
}