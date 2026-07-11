using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IPaymentService
{
    Task<int> CreatePaymentAsync(PaymentCreateDto dto);
    Task UpdatePaymentAsync(int paymentId, PaymentUpdateDto dto);
}

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;

    public PaymentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreatePaymentAsync(PaymentCreateDto dto)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        var exists = await _db.Payments.AnyAsync(p => p.BookingId == dto.BookingId);
        if (exists) throw new InvalidOperationException("Payment already exists for this booking.");

        var p = new Payment
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(p);
        await _db.SaveChangesAsync();
        return p.Id;
    }

    public async Task UpdatePaymentAsync(int paymentId, PaymentUpdateDto dto)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == paymentId)
            ?? throw new KeyNotFoundException("Payment not found.");

        p.TransactionId = dto.TransactionId ?? p.TransactionId;
        p.Status = dto.Status;

        if (dto.Status == PaymentStatus.Paid)
            p.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}