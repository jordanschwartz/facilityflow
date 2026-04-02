namespace FacilityFlow.Application.DTOs.Users;

public record UserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    bool IsAdmin,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime UpdatedAt,
    DateTime? PasswordChangedAt);
