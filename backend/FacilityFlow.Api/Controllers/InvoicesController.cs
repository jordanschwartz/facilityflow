using FacilityFlow.Api.Authorization;
using FacilityFlow.Application.Commands.Invoices;
using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Application.Queries.Invoices;
using FacilityFlow.Core.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[HasPermission(Permission.SendInvoices)]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status, [FromQuery] Guid? clientId,
        [FromQuery] string? location,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(status, clientId, location, dateFrom, dateTo, page, pageSize));
        return Ok(result);
    }

    [HttpGet("billable-work-orders")]
    public async Task<IActionResult> GetBillableWorkOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetBillableWorkOrdersQuery(page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id));
        return Ok(result);
    }

    [HttpPost("{workOrderId:guid}")]
    public async Task<IActionResult> Create(Guid workOrderId, [FromBody] CreateInvoiceRequest req)
    {
        var result = await _mediator.Send(new CreateInvoiceCommand(workOrderId, req));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceRequest req)
    {
        var result = await _mediator.Send(new UpdateInvoiceCommand(id, req));
        return Ok(result);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id)
    {
        var result = await _mediator.Send(new SendInvoiceCommand(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _mediator.Send(new CancelInvoiceCommand(id));
        return Ok(result);
    }

    [HttpPost("stripe-webhook")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> StripeWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _mediator.Send(new HandleStripeWebhookCommand(payload, signature));
        return Ok();
    }
}
