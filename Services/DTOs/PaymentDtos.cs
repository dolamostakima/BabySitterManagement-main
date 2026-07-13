using SmartBabySitter.Models;

namespace SmartBabySitter.Services.DTOs;

public record PaymentCreateDto(
    int BookingId,
    decimal Amount,
    string Method,
    string? TransactionId
);
public record PaymentUpdateDto(string? TransactionId, PaymentStatus Status);