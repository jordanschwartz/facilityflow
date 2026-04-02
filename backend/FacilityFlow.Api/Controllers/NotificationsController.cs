using FacilityFlow.Api.Extensions;
using FacilityFlow.Application.Commands.Notifications;
using FacilityFlow.Application.Queries.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new GetNotificationsQuery(userId));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = User.GetUserId();
        await _mediator.Send(new MarkNotificationReadCommand(id, userId));
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.GetUserId();
        await _mediator.Send(new MarkAllNotificationsReadCommand(userId));
        return NoContent();
    }
}
