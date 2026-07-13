namespace SmartBabySitter.Services.DTOs;

public class PaymentHistoryDto
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public string ParentName { get; set; } = "";

    public string SitterName { get; set; } = "";

    public decimal Amount { get; set; }

    public string Method { get; set; } = "";

    public string Status { get; set; } = "";

    public string? TransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}