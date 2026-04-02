using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string Name, UserRole Role);
