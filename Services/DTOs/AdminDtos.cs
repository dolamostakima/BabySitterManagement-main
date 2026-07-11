using SmartBabySitter.Models;

namespace SmartBabySitter.Services.DTOs;

public record ChangeStatusDto(string ToStatus, string? Note);

public record MarkPaidDto(string Method, string? TransactionId);


public static class BookingWorkflow
{
    public static BookingStatus[] AllowedNext(BookingStatus from) => from switch
    {
        BookingStatus.Pending => new[] { BookingStatus.Accepted, BookingStatus.Rejected, BookingStatus.Cancelled },
        BookingStatus.Accepted => new[] { BookingStatus.Confirmed, BookingStatus.Rejected, BookingStatus.Cancelled },
        BookingStatus.Confirmed => new[] { BookingStatus.Completed, BookingStatus.Cancelled },
        _ => Array.Empty<BookingStatus>()
    };

    public static bool IsValid(BookingStatus from, BookingStatus to)
        => AllowedNext(from).Contains(to) || from == to;
}