using FacilityFlow.Application.Queries.OutboundEmails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class OutboundEmailsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public OutboundEmailsController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    [HttpGet("service-requests/{serviceRequestId:guid}/outbound-emails")]
    public async Task<IActionResult> GetByServiceRequest(
        Guid serviceRequestId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetOutboundEmailsByServiceRequestQuery(serviceRequestId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("outbound-emails/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetOutboundEmailByIdQuery(id));
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("outbound-emails/{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var attachment = await _mediator.Send(new GetOutboundEmailAttachmentQuery(id, attachmentId));
        if (attachment is null)
            return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var physicalPath = Path.Combine(webRoot, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        return PhysicalFile(physicalPath, attachment.ContentType, attachment.FileName);
    }
}
