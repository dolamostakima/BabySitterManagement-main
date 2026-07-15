namespace SmartBabySitter.Services.DTOs;

public class AttendanceCheckInDto
{
    public int BookingId { get; set; }

    public string? LocationText { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}