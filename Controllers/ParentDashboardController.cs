using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services;

namespace SmartBabySitter.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Parent")]
    public class ParentDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        public ParentDashboardController(
            ApplicationDbContext db,
            ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId);

            if (user == null)
                return NotFound();

            // Favorite Sitters
            var favoriteCount = await _db.Favorites
                .CountAsync(x => x.ParentUserId == user.Id);

            // Completed Bookings
            var completedBooking = await _db.Bookings
                .CountAsync(x =>
                    x.ParentUserId == user.Id &&
                    x.Status == BookingStatus.Completed);

            // ================= এখান থেকে নতুন code শুরু =================

            // Total Active Sitters
            var activeSitter = await _db.BabySitterProfiles
                .CountAsync(x => x.IsApproved);

            // Total Organizations
            var organizationCount = await _db.OrganizationRequests
                .CountAsync(x => x.Status == "Approved");

            // Total Notifications
            var notificationCount = await _db.Notifications
                .CountAsync(x => x.ReceiverUserId == user.Id);

            // Latest Notifications
            var notifications = await _db.Notifications
                .Where(x => x.ReceiverUserId == user.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    x.Title,
                    x.Message,
                    x.CreatedAt
                })
                .ToListAsync();

            // ================= নতুন code শেষ =================

            return Ok(new
            {
                fullName = user.FullName,
                address = user.Address,

                favoriteSitters = favoriteCount,
                completedBookings = completedBooking,

                activeSitters = activeSitter,
                organizationCount = organizationCount,

                notificationCount = notificationCount,
                notifications = notifications
            });
        }
    }
}