namespace SmartBabySitter.Models;

public class Booking
{
    public int Id { get; set; }

    // parent (user)
    public int ParentUserId { get; set; }
    public ApplicationUser ParentUser { get; set; } = default!;

    // sitter
    public int BabySitterProfileId { get; set; }
    public BabySitterProfile BabySitterProfile { get; set; } = default!;

    public DateTime BookingDate { get; set; } // date part
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public string ServiceAddressText { get; set; } = "";
    public string? Notes { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // money (optional persisted)
    public decimal? TotalAmount { get; set; }

    // navigation
    public Payment? Payment { get; set; }
    public Attendance? Attendance { get; set; }
    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = new List<BookingStatusHistory>();
    public Review? Review { get; set; }
}