using FacilityFlow.Application.Commands.InboundEmails;
using FacilityFlow.Application.Queries.EmailConversations;
using FacilityFlow.Application.Queries.InboundEmails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class InboundEmailsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public InboundEmailsController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    [HttpGet("service-requests/{serviceRequestId:guid}/email-conversations")]
    public async Task<IActionResult> GetConversations(Guid serviceRequestId)
    {
        var result = await _mediator.Send(new GetEmailConversationsQuery(serviceRequestId));
        return Ok(result);
    }

    [HttpGet("service-requests/{serviceRequestId:guid}/emails")]
    public async Task<IActionResult> GetByServiceRequest(
        Guid serviceRequestId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetInboundEmailsByServiceRequestQuery(serviceRequestId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("inbound-emails/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetInboundEmailByIdQuery(id));
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("inbound-emails/{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await _mediator.Send(new GetInboundEmailAttachmentQuery(id, attachmentId));
        if (attachment is null)
            return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var physicalPath = Path.Combine(webRoot, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        return PhysicalFile(physicalPath, attachment.ContentType, attachment.FileName);
    }

    [HttpPost("inbound-emails/{id:guid}/link/{serviceRequestId:guid}")]
    public async Task<IActionResult> LinkToServiceRequest(Guid id, Guid serviceRequestId)
    {
        var success = await _mediator.Send(new LinkInboundEmailCommand(id, serviceRequestId));
        if (!success)
            return NotFound();
        return NoContent();
    }
}
