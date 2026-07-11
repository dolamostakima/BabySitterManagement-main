using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IBookingService
{
    Task<int> CreateBookingRequestAsync(BookingCreateDto dto);
    Task<BookingDetailsDto> GetBookingAsync(int bookingId);

    Task AcceptAsync(int bookingId, BookingActionDto dto);
    Task RejectAsync(int bookingId, BookingActionDto dto);
    Task ConfirmAsync(int bookingId, BookingActionDto dto);
    Task CompleteAsync(int bookingId, BookingActionDto dto);

    Task<bool> HasConflictAsync(int babySitterProfileId, DateTime date, TimeSpan start, TimeSpan end, int? excludeBookingId = null);
    Task CancelAsync(int bookingId, BookingCancelDto dto);
    Task RescheduleAsync(int bookingId, BookingRescheduleDto dto);
   
    Task<object> GetBookingsAsync(
    BookingStatus? status,
    DateTime? from,
    DateTime? to,
    string? query,
    int page,
    int pageSize);
    Task ChangeBookingStatusAsync(int id, string toStatus, string? note, int adminId);
    Task MarkPaymentPaidAsync(int bookingId, string method, string? trxId);
    Task<object> CheckInAsync(int bookingId);
    Task CheckOutAsync(int bookingId);
}

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;
    private readonly INotificationService _notify;

    public BookingService(ApplicationDbContext db, ICurrentUser me, INotificationService notify)
    {
        _db = db;
        _me = me;
        _notify = notify;
    }


    public async Task<int> CreateBookingRequestAsync(BookingCreateDto dto)
    {
        if (!_me.IsAuthenticated)
            throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == dto.BabySitterProfileId)
            ?? throw new Exception("Sitter not found");

        if (!sitter.IsApproved)
            throw new Exception("Sitter not approved");

        if (dto.EndTime <= dto.StartTime)
            throw new Exception("Invalid time range");

        var hours = (dto.EndTime - dto.StartTime).TotalHours;
        if (hours <= 0)
            throw new Exception("Invalid booking duration");

        // 🔥 Total calculation
        decimal totalAmount = (decimal)hours * sitter.HourlyRate;

        var booking = new Booking
        {
            ParentUserId = _me.UserId,
            BabySitterProfileId = sitter.Id,
            BookingDate = dto.BookingDate.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            ServiceAddressText = dto.ServiceAddressText,
            Notes = dto.Notes,
            Status = BookingStatus.Pending,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        // ✅ Create Payment record
        _db.Payments.Add(new Payment
        {
            BookingId = booking.Id,
            Amount = totalAmount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return booking.Id;
    }

    public async Task MarkPaymentPaidAsync(int bookingId, string method, string? trxId)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
            throw new Exception("Booking not found");

        if (booking.TotalAmount <= 0)
            throw new Exception("Invalid booking amount");

        var payment = await _db.Payments
            .FirstOrDefaultAsync(x => x.BookingId == bookingId);

        // 🔥 যদি payment না থাকে → create
        if (payment == null)
        {
            payment = new Payment
            {
                BookingId = bookingId,
                Amount = booking.TotalAmount.Value,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
        }

        // 🔥 Always sync with booking total
        payment.Amount = booking.TotalAmount.Value;
        payment.Method = method;
        payment.TransactionId = trxId;
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }


    public async Task<BookingDetailsDto> GetBookingAsync(int bookingId)
    {
        var b = await _db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        return new BookingDetailsDto(b.Id, b.ParentUserId, b.BabySitterProfileId, b.BookingDate, b.StartTime, b.EndTime, b.Status, b.TotalAmount, b.CreatedAt);
    }

    public async Task AcceptAsync(int bookingId, BookingActionDto dto)
    {
        var b = await LoadBookingForSitterActionAsync(bookingId);

        if (b.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only Pending booking can be accepted.");

        // conflict check again
        if (await HasConflictAsync(b.BabySitterProfileId, b.BookingDate, b.StartTime, b.EndTime, excludeBookingId: b.Id))
            throw new InvalidOperationException("Time slot already booked.");

        var from = b.Status;
        b.Status = BookingStatus.Accepted;

        await _db.SaveChangesAsync();
        await AddHistoryAsync(b.Id, from, b.Status, _me.UserId, dto.Note);

        await _notify.CreateInAppAsync(b.ParentUserId, "Booking Accepted", $"Your booking request #{b.Id} was accepted.");
    }

    public async Task RejectAsync(int bookingId, BookingActionDto dto)
    {
        var b = await LoadBookingForSitterActionAsync(bookingId);

        if (b.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only Pending booking can be rejected.");

        var from = b.Status;
        b.Status = BookingStatus.Rejected;

        await _db.SaveChangesAsync();
        await AddHistoryAsync(b.Id, from, b.Status, _me.UserId, dto.Note);

        await _notify.CreateInAppAsync(b.ParentUserId, "Booking Rejected", $"Your booking request #{b.Id} was rejected.");
    }

    public async Task ConfirmAsync(int bookingId, BookingActionDto dto)
    {
        // confirm is usually admin/parent after payment; here: sitter can confirm after accept
        var b = await LoadBookingForSitterActionAsync(bookingId);

        if (b.Status != BookingStatus.Accepted)
            throw new InvalidOperationException("Only Accepted booking can be confirmed.");

        var from = b.Status;
        b.Status = BookingStatus.Confirmed;

        await _db.SaveChangesAsync();
        await AddHistoryAsync(b.Id, from, b.Status, _me.UserId, dto.Note);

        await _notify.CreateInAppAsync(b.ParentUserId, "Booking Confirmed", $"Booking #{b.Id} confirmed.");
    }

    public async Task CompleteAsync(int bookingId, BookingActionDto dto)
    {
        var b = await LoadBookingForSitterActionAsync(bookingId);

        if (b.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only Confirmed booking can be completed.");

        var from = b.Status;
        b.Status = BookingStatus.Completed;
        b.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await AddHistoryAsync(b.Id, from, b.Status, _me.UserId, dto.Note);

        await _notify.CreateInAppAsync(b.ParentUserId, "Booking Completed", $"Booking #{b.Id} completed. You can leave a review now.");
    }

    public async Task<bool> HasConflictAsync(int babySitterProfileId, DateTime date, TimeSpan start, TimeSpan end, int? excludeBookingId = null)
    {
        var d = date.Date;

        // conflict bookings: Pending/Accepted/Confirmed (Completed/Cancelled ignored)
        var q = _db.Bookings.Where(b =>
            b.BabySitterProfileId == babySitterProfileId &&
            b.BookingDate == d &&
            b.Status != BookingStatus.Completed &&
            b.Status != BookingStatus.Cancelled &&
            b.Status != BookingStatus.Rejected
        );

        if (excludeBookingId.HasValue)
            q = q.Where(b => b.Id != excludeBookingId.Value);

        // overlap: start < existingEnd && end > existingStart
        return await q.AnyAsync(b => start < b.EndTime && end > b.StartTime);
    }

    private async Task<Booking> LoadBookingForSitterActionAsync(int bookingId)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles.FirstOrDefaultAsync(x => x.UserId == _me.UserId)
            ?? throw new InvalidOperationException("Sitter profile not found.");

        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.BabySitterProfileId == sitter.Id)
            ?? throw new KeyNotFoundException("Booking not found.");

        return b;
    }

    private async Task AddHistoryAsync(int bookingId, BookingStatus from, BookingStatus to, int changedByUserId, string? note)
    {
        _db.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = bookingId,
            FromStatus = from,
            ToStatus = to,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Note = note
        });
        await _db.SaveChangesAsync();
    }

    public async Task CancelAsync(int bookingId, BookingCancelDto dto)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        // Parent or Sitter can cancel (simple rule)
        var sitter = await _db.BabySitterProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == _me.UserId);
        var isParent = b.ParentUserId == _me.UserId;
        var isSitter = sitter != null && b.BabySitterProfileId == sitter.Id;

        if (!isParent && !isSitter)
            throw new UnauthorizedAccessException("You are not allowed to cancel this booking.");

        if (b.Status == BookingStatus.Completed)
            throw new InvalidOperationException("Completed booking cannot be cancelled.");

        if (b.Status == BookingStatus.Cancelled)
            return;

        var from = b.Status;
        b.Status = BookingStatus.Cancelled;

        await _db.SaveChangesAsync();
        await AddHistoryAsync(b.Id, from, b.Status, _me.UserId, dto.Reason);

        // notify both sides
        var sitterUserId = await _db.BabySitterProfiles.Where(s => s.Id == b.BabySitterProfileId).Select(s => s.UserId).FirstAsync();

        await _notify.CreateInAppAsync(b.ParentUserId, "Booking Cancelled", $"Booking #{b.Id} cancelled.");
        await _notify.CreateInAppAsync(sitterUserId, "Booking Cancelled", $"Booking #{b.Id} cancelled.");
    }

    public async Task RescheduleAsync(int bookingId, BookingRescheduleDto dto)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        // Parent can reschedule while Pending/Accepted only (rule)
        if (b.ParentUserId != _me.UserId)
            throw new UnauthorizedAccessException("Only the Parent can reschedule.");

        if (b.Status is BookingStatus.Completed or BookingStatus.Cancelled or BookingStatus.Rejected)
            throw new InvalidOperationException("This booking cannot be rescheduled.");

        if (dto.EndTime <= dto.StartTime)
            throw new InvalidOperationException("EndTime must be greater than StartTime.");

        var date = dto.BookingDate.Date;

        // availability check (recommended)
        var day = date.DayOfWeek;
        var okAvailability = await _db.Availabilities.AnyAsync(a =>
            a.BabySitterProfileId == b.BabySitterProfileId &&
            a.IsAvailable &&
            (
                (a.Date != null && a.Date.Value.Date == date) ||
                (a.Date == null && a.Day == day)
            ) &&
            a.StartTime <= dto.StartTime &&
            a.EndTime >= dto.EndTime
        );

        if (!okAvailability)
            throw new InvalidOperationException("Sitter is not available at that time.");

        // conflict check
        if (await HasConflictAsync(b.BabySitterProfileId, date, dto.StartTime, dto.EndTime, excludeBookingId: b.Id))
            throw new InvalidOperationException("Time slot already booked.");

        var from = b.Status;

        b.BookingDate = date;
        b.StartTime = dto.StartTime;
        b.EndTime = dto.EndTime;

        // reschedule sets status back to Pending (or keep Accepted depending on your policy)
        b.Status = BookingStatus.Pending;

        await _db.SaveChangesAsync();
        await AddHistoryAsync(b.Id, from, b.Status, _me.UserId, dto.Note ?? "Rescheduled");

        var sitterUserId = await _db.BabySitterProfiles.Where(s => s.Id == b.BabySitterProfileId).Select(s => s.UserId).FirstAsync();

        await _notify.CreateInAppAsync(sitterUserId, "Booking Rescheduled",
            $"Booking #{b.Id} rescheduled to {date:yyyy-MM-dd} ({dto.StartTime}-{dto.EndTime}).");
    }


    // ================= BOOKING LIST =================
    public async Task<object> GetBookingsAsync(
        BookingStatus? status,
        DateTime? from,
        DateTime? to,
        string? query,
        int page,
        int pageSize)
    {
        var q = _db.Bookings
            .Include(x => x.ParentUser)
            .Include(x => x.BabySitterProfile).ThenInclude(s => s.User)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (from.HasValue)
            q = q.Where(x => x.BookingDate >= from.Value);

        if (to.HasValue)
            q = q.Where(x => x.BookingDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(x =>
                x.ParentUser.FullName.Contains(query) ||
                x.BabySitterProfile.User.FullName.Contains(query));
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                status = x.Status.ToString(),
                x.BookingDate,
                x.TotalAmount,
                parent = x.ParentUser.FullName,
                sitter = x.BabySitterProfile.User.FullName
            })
            .ToListAsync();

        return new { total, items };
    }

    // ================= STATUS CHANGE =================
    public async Task ChangeBookingStatusAsync(int id, string toStatus, string? note, int adminId)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) throw new KeyNotFoundException("Booking not found.");

        if (!Enum.TryParse<BookingStatus>(toStatus, true, out var to))
            throw new InvalidOperationException("Invalid status value.");

        if (!BookingWorkflow.IsValid(booking.Status, to))
        {
            var allowed = BookingWorkflow.AllowedNext(booking.Status).Select(x => x.ToString()).ToArray();
            throw new InvalidOperationException(
                $"Invalid transition: {booking.Status} -> {to}. Allowed: {string.Join(", ", allowed)}"
            );
        }

        var from = booking.Status;
        booking.Status = to;

        _db.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = id,
            FromStatus = from,
            ToStatus = to,
            ChangedByUserId = adminId,
            Note = note
        });

        await _db.SaveChangesAsync();
    }
  

    // ================= ATTENDANCE =================
    public async Task<object> CheckInAsync(int bookingId)
    {
        var existing = await _db.Attendances.FirstOrDefaultAsync(x => x.BookingId == bookingId);

        if (existing != null)
        {
            return new
            {
                alreadyCheckedIn = true,
                attendanceId = existing.Id,
                checkInTime = existing.CheckInTime
            };
        }

        var att = new Attendance
        {
            BookingId = bookingId,
            CheckInTime = DateTime.UtcNow
        };

        _db.Attendances.Add(att);
        await _db.SaveChangesAsync();

        return new
        {
            alreadyCheckedIn = false,
            attendanceId = att.Id,
            checkInTime = att.CheckInTime
        };
    }

    public async Task CheckOutAsync(int bookingId)
    {
        var att = await _db.Attendances.FirstOrDefaultAsync(x => x.BookingId == bookingId);
        if (att == null) throw new Exception("Not checked in");

        att.CheckOutTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}