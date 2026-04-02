using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<List<User>> GetByRoleAsync(UserRole role)
    {
        return await DbSet.Where(u => u.Role == role).ToListAsync();
    }
}
