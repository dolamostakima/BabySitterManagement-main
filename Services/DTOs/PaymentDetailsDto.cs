namespace SmartBabySitter.Services.DTOs;

public class PaymentDetailsDto
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = "";

    public string? TransactionId { get; set; }

    public string PaymentStatus { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}