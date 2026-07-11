namespace SmartBabySitter.Models;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash"; // Cash, bKash, Nagad, Card
    public string? TransactionId { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}