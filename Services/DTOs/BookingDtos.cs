using SmartBabySitter.Models;

namespace SmartBabySitter.Services.DTOs;

public record BookingCreateDto(
    int BabySitterProfileId,
    DateTime BookingDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string ServiceAddressText,
    string? Notes
);

public record BookingActionDto(string? Note);

public record BookingDetailsDto(
    int BookingId,
    int ParentUserId,
    int BabySitterProfileId,
    DateTime BookingDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    BookingStatus Status,
    decimal? TotalAmount,
    DateTime CreatedAt
);

public record BookingCancelDto(string? Reason);
public record BookingRescheduleDto(DateTime BookingDate, TimeSpan StartTime, TimeSpan EndTime, string? Note);