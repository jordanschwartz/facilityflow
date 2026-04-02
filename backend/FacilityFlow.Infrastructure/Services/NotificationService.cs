using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Infrastructure.Persistence;

namespace FacilityFlow.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public async Task CreateAsync(Guid userId, string type, string message, string? link = null)
    {
        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Message = message,
            Link = link,
            Read = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
