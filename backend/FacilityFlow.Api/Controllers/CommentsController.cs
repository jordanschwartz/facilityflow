using FacilityFlow.Api.Extensions;
using FacilityFlow.Application.Commands.Comments;
using FacilityFlow.Application.DTOs.Comments;
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
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest req)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new CreateCommentCommand(req, userId));
        return CreatedAtAction(nameof(GetComments), result);
    }
}
