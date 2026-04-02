using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Notifications;

public record MarkNotificationReadCommand(Guid Id, Guid UserId) : IRequest<Unit>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly IRepository<Notification> _notifications;

    public MarkNotificationReadCommandHandler(IRepository<Notification> notifications) => _notifications = notifications;

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.Query()
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        notification.Read = true;
        await _notifications.SaveChangesAsync();

        return Unit.Value;
    }
}
