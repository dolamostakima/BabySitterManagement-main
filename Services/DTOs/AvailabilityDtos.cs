namespace SmartBabySitter.Services.DTOs;

public record AvailabilityCreateDto(
    DayOfWeek? Day,   // weekly
    DateTime? Date,   // date specific
    DateTime? EndDate,
TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsAvailable = true
);

public record AvailabilityDto(
    int Id,
    DayOfWeek? Day,
    DateTime? Date,
     DateTime? EndDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsAvailable
);