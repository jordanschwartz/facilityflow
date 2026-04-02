using FacilityFlow.Api.Extensions;
using FacilityFlow.Application.Commands.Vendors;
using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Application.Queries.Vendors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendorsController(IMediator mediator) => _mediator = mediator;

    // ── Vendor CRUD ───────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? trade,
        [FromQuery] string? zip,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDnu,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetVendorsQuery(trade, zip, search, isActive, isDnu, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetVendorByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest req)
    {
        var result = await _mediator.Send(new CreateVendorCommand(req));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorRequest req)
    {
        var result = await _mediator.Send(new UpdateVendorCommand(id, req));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/dnu")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> ToggleDnu(Guid id, [FromBody] ToggleDnuRequest req)
    {
        var result = await _mediator.Send(new ToggleVendorDnuCommand(id, req));
        return Ok(result);
    }

    // ── Nearby / Sourcing ─────────────────────────────────────────────────────

    [HttpGet("nearby")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] string zip,
        [FromQuery] int radiusMiles = 50,
        [FromQuery] string? trade = null)
    {
        if (string.IsNullOrWhiteSpace(zip))
            return BadRequest(new { error = "zip is required." });

        var result = await _mediator.Send(new GetNearbyVendorsQuery(zip, radiusMiles, trade));
        return Ok(result);
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/notes")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetNotes(Guid id)
    {
        var result = await _mediator.Send(new GetVendorNotesQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreateNote(Guid id, [FromBody] CreateVendorNoteRequest req)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new CreateVendorNoteCommand(id, req, userId));
        return CreatedAtAction(nameof(GetNotes), new { id }, result);
    }

    [HttpDelete("{id:guid}/notes/{noteId:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> DeleteNote(Guid id, Guid noteId)
    {
        var userId = User.GetUserId();
        var userRole = User.GetRole();
        await _mediator.Send(new DeleteVendorNoteCommand(id, noteId, userId, userRole));
        return NoContent();
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/payments")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetPayments(Guid id)
    {
        var result = await _mediator.Send(new GetVendorPaymentsQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreatePayment(Guid id, [FromBody] CreateVendorPaymentRequest req)
    {
        var result = await _mediator.Send(new CreateVendorPaymentCommand(id, req));
        return CreatedAtAction(nameof(GetPayments), new { id }, result);
    }

    [HttpPut("{id:guid}/payments/{paymentId:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> UpdatePayment(Guid id, Guid paymentId, [FromBody] UpdateVendorPaymentRequest req)
    {
        var result = await _mediator.Send(new UpdateVendorPaymentCommand(id, paymentId, req));
        return Ok(result);
    }
}
