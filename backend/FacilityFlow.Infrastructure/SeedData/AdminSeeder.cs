using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.SeedData;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(AppDbContext db)
    {
        var adminEmail = "admin@facilityflow.com";
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existing == null)
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "Admin",
                LastName = "Operator",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
        else if (existing.Role != UserRole.Admin)
        {
            // TODO: Migration needed to: UPDATE Users SET Role = 'Admin' WHERE IsAdmin = true; then DROP COLUMN IsAdmin
            existing.Role = UserRole.Admin;
            await db.SaveChangesAsync();
        }
    }
}
