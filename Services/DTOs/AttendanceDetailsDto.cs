namespace SmartBabySitter.Services.DTOs;

public class AttendanceDetailsDto
{
    public int AttendanceId { get; set; }

    public int BookingId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? LocationText { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}