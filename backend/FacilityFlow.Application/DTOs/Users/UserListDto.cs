namespace FacilityFlow.Application.DTOs.Users;

public record UserListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
