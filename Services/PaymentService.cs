using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IPaymentService
{
    Task<int> CreatePaymentAsync(PaymentCreateDto dto);
    Task UpdatePaymentAsync(int paymentId, PaymentUpdateDto dto);

    Task<PaymentDetailsDto?> GetPaymentByBookingAsync(int bookingId);

    Task<List<PaymentHistoryDto>> GetParentPaymentHistoryAsync(int parentUserId);

    Task<List<PaymentHistoryDto>> GetSitterPaymentHistoryAsync(int sitterUserId);
}

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public PaymentService(
        ApplicationDbContext db,
        INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<int> CreatePaymentAsync(PaymentCreateDto dto)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.Status != BookingStatus.Accepted &&
    booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Payment is allowed only for accepted or confirmed bookings.");
        }

        var payment = await _db.Payments
     .FirstOrDefaultAsync(p => p.BookingId == dto.BookingId);

        if (payment != null)
        {
            if (payment.Status == PaymentStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Your payment request is already pending for admin approval.");
            }

            if (payment.Status == PaymentStatus.Paid)
            {
                throw new InvalidOperationException(
                    "This booking has already been paid.");
            }

            if (payment.Status == PaymentStatus.Failed)
            {
                payment.Amount = dto.Amount;
                payment.Method = dto.Method;
                payment.TransactionId = dto.TransactionId;
                payment.Status = PaymentStatus.Pending;
                payment.CreatedAt = DateTime.UtcNow;
                payment.PaidAt = null;

                booking.Status = BookingStatus.PaymentPending;

                await _db.SaveChangesAsync();

                return payment.Id;
            }
        }

        var p = new Payment
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            Method = dto.Method,
            TransactionId = dto.TransactionId,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(p);

        // Booking status update
        booking.Status = BookingStatus.PaymentPending;

        await _db.SaveChangesAsync();

        return p.Id;
    }

    public async Task UpdatePaymentAsync(int paymentId, PaymentUpdateDto dto)
    {
        var payment = await _db.Payments
    .Include(p => p.Booking)
        .ThenInclude(b => b.ParentUser)
    .Include(p => p.Booking)
        .ThenInclude(b => b.BabySitterProfile)
            .ThenInclude(s => s.User)
    .FirstOrDefaultAsync(x => x.Id == paymentId)
    ?? throw new KeyNotFoundException("Payment not found.");

        payment.TransactionId = dto.TransactionId ?? payment.TransactionId;
        payment.Status = dto.Status;

        

        if (dto.Status == PaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;

            payment.Booking.Status = BookingStatus.Confirmed;

            await _notifications.CreateInAppAsync(
                payment.Booking.ParentUserId,
                "Payment Approved",
                $"Your payment for Booking #{payment.Booking.Id} has been approved successfully.");

            await _notifications.CreateInAppAsync(
                payment.Booking.BabySitterProfile.UserId,
                "Payment Received",
                $"Payment has been approved for Booking #{payment.Booking.Id}.");
        }

        else if (dto.Status == PaymentStatus.Failed)
        {
            payment.Booking.Status = BookingStatus.Accepted;

            await _notifications.CreateInAppAsync(
                payment.Booking.ParentUserId,
                "Payment Failed",
                $"Your payment for Booking #{payment.Booking.Id} was rejected. Please submit payment again.");

            await _notifications.CreateInAppAsync(
                payment.Booking.BabySitterProfile.UserId,
                "Payment Failed",
                $"Payment for Booking #{payment.Booking.Id} was not approved.");
        }

        else if (dto.Status == PaymentStatus.Rejected)
        {
            payment.Booking.Status = BookingStatus.Accepted;

            await _notifications.CreateInAppAsync(
                payment.Booking.ParentUserId,
                "Payment Rejected",
                $"Your payment for Booking #{payment.Booking.Id} was rejected. Please submit payment again.");

            await _notifications.CreateInAppAsync(
                payment.Booking.BabySitterProfile.UserId,
                "Payment Rejected",
                $"Payment for Booking #{payment.Booking.Id} was rejected by admin.");
        }

        await _db.SaveChangesAsync();
    }

    public async Task<PaymentDetailsDto?> GetPaymentByBookingAsync(int bookingId)
    {
        return await _db.Payments
            .AsNoTracking()
            .Where(p => p.BookingId == bookingId)
            .Select(p => new PaymentDetailsDto
            {
                PaymentId = p.Id,
                BookingId = p.BookingId,
                Amount = p.Amount,
                Method = p.Method,
                TransactionId = p.TransactionId,
                PaymentStatus = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            })
            .FirstOrDefaultAsync();

            
    }

    public async Task<List<PaymentHistoryDto>> GetParentPaymentHistoryAsync(int parentUserId)
    {
        return await _db.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.ParentUser)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BabySitterProfile)
                    .ThenInclude(s => s.User)
            .Where(p => p.Booking.ParentUserId == parentUserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentHistoryDto
            {
                PaymentId = p.Id,
                BookingId = p.BookingId,
                ParentName = p.Booking.ParentUser.FullName,
                SitterName = p.Booking.BabySitterProfile.User.FullName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status.ToString(),
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            })
            .ToListAsync();
    }

    public async Task<List<PaymentHistoryDto>> GetSitterPaymentHistoryAsync(int sitterUserId)
    {
        return await _db.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.ParentUser)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BabySitterProfile)
                    .ThenInclude(s => s.User)
            .Where(p => p.Booking.BabySitterProfile.UserId == sitterUserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentHistoryDto
            {
                PaymentId = p.Id,
                BookingId = p.BookingId,
                ParentName = p.Booking.ParentUser.FullName,
                SitterName = p.Booking.BabySitterProfile.User.FullName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status.ToString(),
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            })
            .ToListAsync();
    }
}

