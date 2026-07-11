namespace SmartBabySitter.Models;

public class Attendance
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public string? LocationText { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}