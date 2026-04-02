using FacilityFlow.Core.DTOs.Auth;

namespace FacilityFlow.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest req);
    Task<AuthResponse> LoginAsync(LoginRequest req);
    Task<UserDto> GetMeAsync(Guid userId);
}
