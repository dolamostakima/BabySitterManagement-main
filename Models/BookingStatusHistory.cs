namespace SmartBabySitter.Models;

public class BookingStatusHistory
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }

    public int ChangedByUserId { get; set; }
    public ApplicationUser ChangedByUser { get; set; } = default!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}