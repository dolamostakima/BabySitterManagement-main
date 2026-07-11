using SmartBabySitter.Models;

namespace SmartBabySitter.Services.DTOs;

public record PaymentCreateDto(int BookingId, decimal Amount, string Method);
public record PaymentUpdateDto(string? TransactionId, PaymentStatus Status);