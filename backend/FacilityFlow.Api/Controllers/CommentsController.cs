using FacilityFlow.Api.Extensions;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.DTOs.Comments;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetComments(
        [FromQuery] Guid? serviceRequestId,
        [FromQuery] Guid? quoteId,
        [FromQuery] Guid? workOrderId)
    {
        var query = _db.Comments.Include(c => c.Author).AsQueryable();

        if (serviceRequestId.HasValue)
            query = query.Where(c => c.ServiceRequestId == serviceRequestId.Value);
        else if (quoteId.HasValue)
            query = query.Where(c => c.QuoteId == quoteId.Value);
        else if (workOrderId.HasValue)
            query = query.Where(c => c.WorkOrderId == workOrderId.Value);
        else
            return BadRequest(new { error = "One of serviceRequestId, quoteId, or workOrderId must be provided." });

        var comments = await query.OrderBy(c => c.CreatedAt).ToListAsync();
        var dtos = comments.Select(c => new CommentDto(
            c.Id,
            c.Text,
            c.AuthorId,
            c.ServiceRequestId,
            c.QuoteId,
            c.WorkOrderId,
            c.CreatedAt,
            c.Author.Adapt<UserDto>()
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest req)
    {
        // Validate exactly one context is provided
        var provided = new[] { req.ServiceRequestId.HasValue, req.QuoteId.HasValue, req.WorkOrderId.HasValue }
            .Count(v => v);

        if (provided != 1)
            return BadRequest(new { error = "Exactly one of serviceRequestId, quoteId, or workOrderId must be provided." });

        var userId = User.GetUserId();

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = req.Text,
            AuthorId = userId,
            ServiceRequestId = req.ServiceRequestId,
            QuoteId = req.QuoteId,
            WorkOrderId = req.WorkOrderId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var author = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var dto = new CommentDto(
            comment.Id,
            comment.Text,
            comment.AuthorId,
            comment.ServiceRequestId,
            comment.QuoteId,
            comment.WorkOrderId,
            comment.CreatedAt,
            author.Adapt<UserDto>()
        );

        return CreatedAtAction(nameof(GetComments), dto);
    }
}
