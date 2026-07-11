namespace SmartBabySitter.Services.DTOs;

public record RegisterRequestDto(string FullName,
    string? nidNo,
    string? gender,
    string? dateOfBirth,
    string? address,
    string? Email,
    string? phoneNo,
    string? Password,
    int? type, // 1 for parent, 2 for babysitter
    string? Experience,
    string Role
    
    
    
    );
public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    int UserId,
    string? FullName,
    string Email,
   // string? nidNo,
   // string? gender,
    //string? dateOfBirth,
    //string? address,
   // string? phoneNo,
   // string? Password,
   // int? type, // 1 for parent, 2 for babysitter
    //string? Experience,
    IList<string> Roles
);