using FacilityFlow.Core.Entities;

namespace FacilityFlow.Core.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
