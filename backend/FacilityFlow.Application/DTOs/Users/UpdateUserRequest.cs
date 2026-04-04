namespace FacilityFlow.Application.DTOs.Users;

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Status,
    string? Role = null);
