namespace SmartBabySitter.Models;

public class Review
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public int ParentUserId { get; set; }
    public ApplicationUser ParentUser { get; set; } = default!;

    public int BabySitterProfileId { get; set; }
    public BabySitterProfile BabySitterProfile { get; set; } = default!;

    public int Rating { get; set; } // 1–5
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsApproved { get; set; } = false; // Admin approve করলে true
    public bool IsHidden { get; set; } = false;   // Admin hide করলে true

    public string? SitterReply { get; set; }
    public DateTime? SitterReplyAt { get; set; }
    public int? SitterReplyByUserId { get; set; }
    public DateTime? SitterReplyUpdatedAt { get; set; }
}