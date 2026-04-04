using FacilityFlow.Api.Authorization;
using FacilityFlow.Application.Commands.Quotes;
using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Application.Queries.Quotes;
using FacilityFlow.Core.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuotesController(IMediator mediator)
        => _mediator = mediator;

    [HttpGet("api/service-requests/{serviceRequestId:guid}/quotes")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> GetQuotes(Guid serviceRequestId)
        => Ok(await _mediator.Send(new GetQuotesByServiceRequestQuery(serviceRequestId)));

    [HttpPost("api/service-requests/{serviceRequestId:guid}/quotes")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> CreateQuote(Guid serviceRequestId, [FromBody] SubmitQuoteRequest req)
    {
        var result = await _mediator.Send(new CreateQuoteCommand(serviceRequestId, req));
        return CreatedAtAction(nameof(GetQuotes), new { serviceRequestId }, result);
    }

    [HttpPatch("api/quotes/{id:guid}/status")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuoteStatusRequest req)
        => Ok(await _mediator.Send(new UpdateQuoteStatusCommand(id, req)));

    [HttpGet("api/quotes/submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByToken(string token)
        => Ok(await _mediator.Send(new GetQuoteByTokenQuery(token)));

    [HttpPost("api/quotes/submit/{token}/attachments")]
    [AllowAnonymous]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> UploadAttachment(string token, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadQuoteAttachmentCommand(token, stream, file.FileName, file.ContentType));
        return Ok(result);
    }

    [HttpDelete("api/quotes/submit/{token}/attachments/{attachmentId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteAttachment(string token, Guid attachmentId)
    {
        await _mediator.Send(new DeleteQuoteAttachmentCommand(token, attachmentId));
        return NoContent();
    }

    [HttpPost("api/quotes/submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitByToken(string token, [FromBody] SubmitQuoteRequest req)
        => Ok(await _mediator.Send(new SubmitQuoteByTokenCommand(token, req)));

    [HttpPost("api/service-requests/{serviceRequestId:guid}/quotes/unselect")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> UnselectQuote(Guid serviceRequestId)
    {
        await _mediator.Send(new UnselectQuoteCommand(serviceRequestId));
        return NoContent();
    }

    [HttpPost("api/quotes/manual-entry")]
    [HasPermission(Permission.EditWorkOrders)]
    public async Task<IActionResult> ManualEntry([FromBody] ManualQuoteEntryRequest req)
    {
        var quoteId = await _mediator.Send(new ManualQuoteEntryCommand(
            req.ServiceRequestId,
            req.VendorInviteId,
            req.Price,
            req.ScopeOfWork,
            req.ProposedStartDate,
            req.EstimatedDurationValue,
            req.EstimatedDurationUnit,
            req.NotToExceedPrice,
            req.Assumptions,
            req.Exclusions,
            req.VendorAvailability,
            req.ValidUntil,
            req.LineItems?.Select(li => new ManualQuoteLineItem(li.Description, li.Quantity, li.UnitPrice)).ToList()
        ));
        return Ok(new { id = quoteId });
    }
}
