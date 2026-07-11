using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IReviewService
{
    Task<object> CanReviewAsync(int bookingId);
    Task<int> CreateAsync(ReviewCreateDto dto);

    // ✅ Parent list
    Task<object> GetMyReviewsAsync();
    Task<object> GetMyReceivedReviewsAsync(int page, int pageSize);

    // ✅ Admin moderation
    Task<object> AdminListAsync(bool? approved, bool? hidden, int page, int pageSize, string? query);
    Task AdminDecisionAsync(int reviewId, ReviewAdminDecisionDto dto, int adminId);

    Task ReplyAsync(int reviewId, ReviewReplyDto dto);
}

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;

    public ReviewService(ApplicationDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    // ✅ Create: default IsApproved=false so admin must approve
    public async Task<int> CreateAsync(ReviewCreateDto dto)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        if (dto.Rating < 1 || dto.Rating > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var b = await _db.Bookings
            .FirstOrDefaultAsync(x => x.Id == dto.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (b.ParentUserId != _me.UserId)
            throw new UnauthorizedAccessException("Not your booking.");

        if (b.Status != BookingStatus.Completed)
            throw new InvalidOperationException("You can review only after booking is completed.");

        var exists = await _db.Reviews.AnyAsync(r => r.BookingId == b.Id);
        if (exists) throw new InvalidOperationException("Review already submitted.");

        var review = new Review
        {
            BookingId = b.Id,
            ParentUserId = _me.UserId,
            BabySitterProfileId = b.BabySitterProfileId,
            Rating = dto.Rating,
            Comment = (dto.Comment ?? "").Trim(),
            CreatedAt = DateTime.UtcNow,
            IsApproved = false,
            IsHidden = false
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review.Id;
    }

    // ✅ Parent: My Reviews list
    public async Task<object> GetMyReviewsAsync()
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var items = await _db.Reviews.AsNoTracking()
            .Where(r => r.ParentUserId == _me.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.BookingId,
                r.BabySitterProfileId,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                r.IsApproved,
                r.IsHidden,
                r.SitterReply,
                r.SitterReplyAt
            })
            .ToListAsync();

        return new { total = items.Count, items };
    }

    // ✅ Admin: Review list (filter/search/paging)
    public async Task<object> AdminListAsync(bool? approved, bool? hidden, int page, int pageSize, string? query)
    {
        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var q = _db.Reviews.AsNoTracking()
            .Include(r => r.ParentUser)
            .Include(r => r.BabySitterProfile).ThenInclude(s => s.User)
            .AsQueryable();

        if (approved.HasValue) q = q.Where(x => x.IsApproved == approved.Value);
        if (hidden.HasValue) q = q.Where(x => x.IsHidden == hidden.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            q = q.Where(r =>
                (r.ParentUser.FullName ?? "").Contains(query) ||
                (r.ParentUser.Email ?? "").Contains(query) ||
                (r.BabySitterProfile.User.FullName ?? "").Contains(query) ||
                (r.BabySitterProfile.User.Email ?? "").Contains(query) ||
                (r.Comment ?? "").Contains(query)
            );
        }

        var total = await q.CountAsync();

        var items = await q.OrderByDescending(x => x.CreatedAt)
            .Skip((p - 1) * size).Take(size)
            .Select(r => new
            {
                r.Id,
                r.BookingId,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                r.IsApproved,
                r.IsHidden,
                r.SitterReply,
                r.SitterReplyAt,
                Parent = new { r.ParentUserId, r.ParentUser.FullName, r.ParentUser.Email },
                Sitter = new { r.BabySitterProfileId, r.BabySitterProfile.User.FullName, r.BabySitterProfile.User.Email }
            })
            .ToListAsync();

        return new { total, page = p, pageSize = size, items };
    }

    // ✅ Admin approve/hide
    public async Task AdminDecisionAsync(int reviewId, ReviewAdminDecisionDto dto, int adminId)
    {
        var r = await _db.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId)
            ?? throw new KeyNotFoundException("Review not found.");

        r.IsApproved = dto.Approve;
        r.IsHidden = dto.Hide;

        await _db.SaveChangesAsync();
    }

    public async Task<object> CanReviewAsync(int bookingId)
    {
        if (!_me.IsAuthenticated) return new { can = false, reason = "Not authenticated" };

        var b = await _db.Bookings
            .AsNoTracking()
            .Include(x => x.BabySitterProfile).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (b == null) return new { can = false, reason = "Booking not found" };
        if (b.ParentUserId != _me.UserId) return new { can = false, reason = "Not your booking" };
        if (b.Status != BookingStatus.Completed) return new { can = false, reason = "Booking not completed yet" };

        var exists = await _db.Reviews.AnyAsync(r => r.BookingId == bookingId);
        if (exists) return new { can = false, reason = "Review already submitted" };

        return new
        {
            can = true,
            booking = new
            {
                b.Id,
                status = b.Status.ToString(),
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                b.TotalAmount,
                sitter = new
                {
                    b.BabySitterProfileId,
                    fullName = b.BabySitterProfile.User.FullName,
                    email = b.BabySitterProfile.User.Email
                }
            }
        };
    }

    public async Task<object> GetMyReceivedReviewsAsync(int page, int pageSize)
    {
        if (!_me.IsAuthenticated)
            throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == _me.UserId);

        if (sitter == null)
            throw new InvalidOperationException("Sitter profile not found.");

        var p = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var q = _db.Reviews
            .AsNoTracking()
            .Include(r => r.ParentUser)
            .Include(r => r.Booking)
            .Where(r => r.BabySitterProfileId == sitter.Id
                        && r.IsApproved
                        && !r.IsHidden);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((p - 1) * size)
            .Take(size)
           .Select(r => new
           {
               r.Id,
               r.BookingId,
               r.Rating,
               r.Comment,
               r.CreatedAt,
               r.IsApproved,
               r.IsHidden,
               r.SitterReply,
               r.SitterReplyAt,
               parent = new
               {
                   r.ParentUserId,
                   fullName = r.ParentUser.FullName,
                   email = r.ParentUser.Email
               },
               booking = new
               {
                   r.Booking.Id,
                   r.Booking.BookingDate,
                   r.Booking.StartTime,
                   r.Booking.EndTime,
                   status = r.Booking.Status.ToString()
               }
            })
            .ToListAsync();

        var avgRating = items.Count == 0 ? 0 : Math.Round(items.Average(x => x.Rating), 1);
        var fiveStarReviews = items.Count(x => x.Rating == 5);

        return new
        {
            total,
            page = p,
            pageSize = size,
            avgRating,
            fiveStarReviews,
            items
        };
    }

    public async Task ReplyAsync(int reviewId, ReviewReplyDto dto)
    {
        if (!_me.IsAuthenticated)
            throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles
            .FirstOrDefaultAsync(x => x.UserId == _me.UserId);

        if (sitter == null)
            throw new InvalidOperationException("Sitter profile not found.");

        var review = await _db.Reviews
            .FirstOrDefaultAsync(x => x.Id == reviewId)
            ?? throw new KeyNotFoundException("Review not found.");

        if (review.BabySitterProfileId != sitter.Id)
            throw new UnauthorizedAccessException("You cannot reply to this review.");

        if (!review.IsApproved || review.IsHidden)
            throw new InvalidOperationException("You can reply only to approved visible reviews.");

        var reply = (dto.Reply ?? "").Trim();
        if (string.IsNullOrWhiteSpace(reply))
            throw new InvalidOperationException("Reply is required.");

        if (reply.Length > 1000)
            throw new InvalidOperationException("Reply is too long.");

        review.SitterReply = reply;
        review.SitterReplyAt = DateTime.UtcNow;
        review.SitterReplyByUserId = _me.UserId;

        await _db.SaveChangesAsync();
    }

}