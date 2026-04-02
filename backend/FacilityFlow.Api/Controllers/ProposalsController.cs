using FacilityFlow.Application.Commands.Proposals;
using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Application.Queries.Proposals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Authorize]
public class ProposalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProposalsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("api/service-requests/{serviceRequestId:guid}/proposals")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create(Guid serviceRequestId, [FromBody] CreateProposalRequest req)
    {
        var result = await _mediator.Send(new CreateProposalCommand(serviceRequestId, req));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("api/service-requests/{serviceRequestId:guid}/proposals")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetByServiceRequest(Guid serviceRequestId)
    {
        var result = await _mediator.Send(new GetProposalByServiceRequestQuery(serviceRequestId));
        if (result == null) return NoContent();
        return Ok(result);
    }

    [HttpGet("api/proposals/{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProposalByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("api/proposals/{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProposalRequest req)
    {
        var result = await _mediator.Send(new UpdateProposalCommand(id, req));
        return Ok(result);
    }

    [HttpPost("api/proposals/{id:guid}/send")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Send(Guid id)
    {
        var result = await _mediator.Send(new SendProposalCommand(id));
        return Ok(result);
    }

    [HttpPost("api/proposals/{id:guid}/generate-summary")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GenerateSummary(Guid id, [FromBody] GenerateSummaryRequest req)
    {
        var result = await _mediator.Send(new GenerateProposalSummaryCommand(id, req));
        return Ok(result);
    }

    [HttpPost("api/proposals/{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondToProposalRequest req)
    {
        var result = await _mediator.Send(new RespondToProposalCommand(id, req));
        return Ok(result);
    }

    [HttpGet("api/proposals/view/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewByToken(string token)
    {
        var result = await _mediator.Send(new GetProposalByTokenQuery(token));
        return Ok(result);
    }

    [HttpGet("api/proposals/{id:guid}/versions")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetVersions(Guid id)
    {
        var result = await _mediator.Send(new GetProposalVersionsQuery(id));
        return Ok(result);
    }
}
