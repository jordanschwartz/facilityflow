using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, UserRole Role);
