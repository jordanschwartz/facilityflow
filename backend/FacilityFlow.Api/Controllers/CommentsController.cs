using FacilityFlow.Api.Extensions;
using FacilityFlow.Application.Commands.Comments;
using FacilityFlow.Application.Queries.Comments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetComments(
        [FromQuery] Guid? serviceRequestId,
        [FromQuery] Guid? quoteId,
        [FromQuery] Guid? workOrderId)
    {
        if (!serviceRequestId.HasValue && !quoteId.HasValue && !workOrderId.HasValue)
            return BadRequest(new { error = "One of serviceRequestId, quoteId, or workOrderId must be provided." });

        var result = await _mediator.Send(new GetCommentsQuery(serviceRequestId, quoteId, workOrderId));
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Create(
        [FromForm] string text,
        [FromForm] Guid? serviceRequestId,
        [FromForm] Guid? quoteId,
        [FromForm] Guid? workOrderId,
        List<IFormFile>? files)
    {
        var userId = User.GetUserId();

        var attachments = files?.Select(f => new CommentAttachmentInput(
            f.OpenReadStream(), f.FileName, f.ContentType
        )).ToList();

        var result = await _mediator.Send(new CreateCommentCommand(
            text, userId, serviceRequestId, quoteId, workOrderId, attachments));

        return CreatedAtAction(nameof(GetComments), result);
    }
}
