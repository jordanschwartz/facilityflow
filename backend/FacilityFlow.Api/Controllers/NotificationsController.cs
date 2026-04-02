using FacilityFlow.Api.Extensions;
using FacilityFlow.Core.DTOs.Notifications;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.GetUserId();

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var items = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Type,
            n.Message,
            n.Read,
            n.Link,
            n.CreatedAt
        )).ToList();

        var unreadCount = notifications.Count(n => !n.Read);

        return Ok(new NotificationsResponse(items, unreadCount, notifications.Count));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = User.GetUserId();

        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId)
            ?? throw new NotFoundException("Notification not found.");

        notification.Read = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetUserId();

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .ToListAsync();

        foreach (var n in unread)
            n.Read = true;

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
