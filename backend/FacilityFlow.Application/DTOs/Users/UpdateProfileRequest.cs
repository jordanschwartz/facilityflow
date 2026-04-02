namespace FacilityFlow.Application.DTOs.Users;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string Email);
