using FacilityFlow.Application.Commands.Clients;
using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Application.Queries.Clients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetClientsQuery(search, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest req)
    {
        var result = await _mediator.Send(new CreateClientCommand(req));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetClientByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest req)
    {
        var result = await _mediator.Send(new UpdateClientCommand(id, req));
        return Ok(result);
    }
}
