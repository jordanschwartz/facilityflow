using FacilityFlow.Api.Authorization;
using FacilityFlow.Application.Queries.Dashboard;
using FacilityFlow.Core.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[HasPermission(Permission.CreateWorkOrders)]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline()
    {
        var result = await _mediator.Send(new GetPipelineQuery());
        return Ok(result);
    }
}
