namespace FacilityFlow.Core.DTOs.Auth;

public record UserDto(Guid Id, string Email, string Name, string Role, DateTime CreatedAt);
