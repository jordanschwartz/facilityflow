namespace FacilityFlow.Application.DTOs.Users;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Password,
    string Role);
