using FacilityFlow.Api.Authorization;
using FacilityFlow.Api.Extensions;
using FacilityFlow.Application.Commands.ServiceRequests;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Application.DTOs.VendorInvites;
using FacilityFlow.Application.Queries.ServiceRequests;
using FacilityFlow.Core.Enums;
using FacilityFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;

    public ServiceRequestsController(IMediator mediator, AppDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? clientId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetServiceRequestsQuery(status, priority, clientId, search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        var categories = await _db.ServiceRequests
            .Where(sr => sr.Category != null && sr.Category != "")
            .Select(sr => sr.Category!)
            .Distinct()
            .ToListAsync();

        var trades = await _db.Vendors
            .Where(v => v.Trades != null)
            .SelectMany(v => v.Trades)
            .Distinct()
            .ToListAsync();

        var merged = categories.Concat(trades)
            .GroupBy(s => s.ToLowerInvariant())
            .Select(g => g.First())
            .OrderBy(s => s)
            .ToList();

        return Ok(merged);
    }

    [HttpPost]
    [HasPermission(Permission.CreateWorkOrders)]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestRequest req)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new CreateServiceRequestCommand(req, userId));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetServiceRequestByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequestRequest req)
    {
        var result = await _mediator.Send(new UpdateServiceRequestCommand(id, req));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateServiceRequestStatusRequest req)
    {
        var result = await _mediator.Send(new UpdateServiceRequestStatusCommand(id, req));
        return Ok(result);
    }

    [HttpPost("{id:guid}/invites")]
    [HasPermission(Permission.SendWorkOrders)]
    public async Task<IActionResult> CreateInvites(Guid id, [FromBody] CreateVendorInvitesRequest req)
    {
        var result = await _mediator.Send(new CreateVendorInvitesCommand(id, req));
        return Ok(result);
    }

    [HttpGet("{id:guid}/invites")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> GetInvites(Guid id)
    {
        var result = await _mediator.Send(new GetVendorInvitesQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/upload-po")]
    [HasPermission(Permission.EditWorkOrders)]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> UploadPo(Guid id, [FromForm] string poNumber, [FromForm] decimal? poAmount, IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadPoCommand(id, poNumber, poAmount, stream, file.FileName, file.ContentType));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/schedule")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateScheduleCommand cmd)
    {
        var result = await _mediator.Send(new UpdateScheduleCommand(id, cmd.ScheduledDate));
        return Ok(result);
    }

    [HttpGet("{id:guid}/allowed-transitions")]
    public async Task<IActionResult> GetAllowedTransitions(Guid id)
    {
        var result = await _mediator.Send(new GetAllowedTransitionsQuery(id));
        return Ok(result);
    }
}
