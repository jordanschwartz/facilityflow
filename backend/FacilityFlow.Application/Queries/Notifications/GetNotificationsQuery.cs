using FacilityFlow.Application.DTOs.Notifications;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Notifications;

public record GetNotificationsQuery(Guid UserId) : IRequest<NotificationsResponse>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationsResponse>
{
    private readonly IRepository<Notification> _notifications;

    public GetNotificationsQueryHandler(IRepository<Notification> notifications) => _notifications = notifications;

    public async Task<NotificationsResponse> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notifications.Query()
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Type,
            n.Message,
            n.Read,
            n.Link,
            n.CreatedAt
        )).ToList();

        var unreadCount = notifications.Count(n => !n.Read);

        return new NotificationsResponse(items, unreadCount, notifications.Count);
    }
}
