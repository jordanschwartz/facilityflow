using FacilityFlow.Application.Commands.EmailActions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EmailActionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailActionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("inbound-emails/{id:guid}/actions/create-quote")]
    public async Task<IActionResult> CreateQuoteFromEmail(Guid id)
    {
        var result = await _mediator.Send(new CreateQuoteFromEmailCommand(id));
        return Ok(new { result.QuoteId });
    }

    [HttpPost("inbound-emails/{id:guid}/actions/attach-as-po")]
    public async Task<IActionResult> AttachAsPurchaseOrder(Guid id, [FromBody] AttachAsPurchaseOrderRequest request)
    {
        await _mediator.Send(new AttachEmailAsPurchaseOrderCommand(id, request.AttachmentId));
        return Ok();
    }

    [HttpPost("inbound-emails/{id:guid}/actions/add-to-notes")]
    public async Task<IActionResult> AddToNotes(Guid id)
    {
        await _mediator.Send(new AddEmailToNotesCommand(id));
        return Ok();
    }

    [HttpPost("outbound-emails/{id:guid}/actions/resend")]
    public async Task<IActionResult> ResendEmail(Guid id)
    {
        await _mediator.Send(new ResendOutboundEmailCommand(id));
        return Ok();
    }

    [HttpPost("outbound-emails/{id:guid}/actions/forward")]
    public async Task<IActionResult> ForwardEmail(Guid id, [FromBody] ForwardEmailRequest request)
    {
        await _mediator.Send(new ForwardOutboundEmailCommand(id, request.RecipientEmail, request.RecipientName));
        return Ok();
    }
}

public record AttachAsPurchaseOrderRequest(Guid AttachmentId);
public record ForwardEmailRequest(string RecipientEmail, string? RecipientName);
