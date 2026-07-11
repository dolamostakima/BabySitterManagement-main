namespace SmartBabySitter.Services.DTOs;

public record ReviewCreateDto(
    int BookingId,
    int Rating,      // 1..5
    string? Comment
);

public record ReviewAdminDecisionDto(bool Approve, bool Hide = false, string? Note = null);

public class ReviewReplyDto
{
    public string? Reply { get; set; }
}