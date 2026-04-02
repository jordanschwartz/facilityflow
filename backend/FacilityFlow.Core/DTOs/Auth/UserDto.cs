namespace FacilityFlow.Core.DTOs.Auth;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Name,
    string Role,
    bool IsAdmin,
    string Status,
    DateTime CreatedAt);
