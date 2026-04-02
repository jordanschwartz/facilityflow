using FacilityFlow.Application.Commands.WorkOrders;
using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Application.Queries.WorkOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? vendorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetWorkOrdersQuery(status, vendorId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkOrderByIdQuery(id));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Operator,Vendor")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWorkOrderStatusRequest req)
    {
        var result = await _mediator.Send(new UpdateWorkOrderStatusCommand(id, req));
        return Ok(result);
    }
}
