using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Notifications;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Unit>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Unit>
{
    private readonly IRepository<Notification> _notifications;

    public MarkAllNotificationsReadCommandHandler(IRepository<Notification> notifications) => _notifications = notifications;

    public async Task<Unit> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _notifications.Query()
            .Where(n => n.UserId == request.UserId && !n.Read)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
            n.Read = true;

        await _notifications.SaveChangesAsync();

        return Unit.Value;
    }
}
