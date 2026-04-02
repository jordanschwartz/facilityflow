using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<List<User>> GetByRoleAsync(UserRole role);
}
