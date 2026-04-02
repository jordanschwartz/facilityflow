using FacilityFlow.Application.Queries.ActivityLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/activity-logs")]
[Authorize]
public class ActivityLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActivityLogsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] Guid serviceRequestId,
        [FromQuery] Guid? workOrderId,
        [FromQuery] string? category)
    {
        var result = await _mediator.Send(new GetActivityLogsQuery(serviceRequestId, workOrderId, category));
        return Ok(result);
    }
}
