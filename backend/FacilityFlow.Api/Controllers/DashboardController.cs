using FacilityFlow.Application.Queries.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Operator")]
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
