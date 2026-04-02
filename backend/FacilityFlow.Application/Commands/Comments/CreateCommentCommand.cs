using FacilityFlow.Application.DTOs.Comments;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.Comments;

public record CreateCommentCommand(CreateCommentRequest Request, Guid UserId) : IRequest<CommentDto>;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly IRepository<Comment> _comments;
    private readonly IRepository<User> _users;

    public CreateCommentCommandHandler(IRepository<Comment> comments, IRepository<User> users)
    {
        _comments = comments;
        _users = users;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var provided = new[] { req.ServiceRequestId.HasValue, req.QuoteId.HasValue, req.WorkOrderId.HasValue }
            .Count(v => v);

        if (provided != 1)
            throw new ArgumentException("Exactly one of serviceRequestId, quoteId, or workOrderId must be provided.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = req.Text,
            AuthorId = command.UserId,
            ServiceRequestId = req.ServiceRequestId,
            QuoteId = req.QuoteId,
            WorkOrderId = req.WorkOrderId,
            CreatedAt = DateTime.UtcNow
        };

        _comments.Add(comment);
        await _comments.SaveChangesAsync();

        var author = await _users.GetByIdAsync(command.UserId)
            ?? throw new NotFoundException("User not found.");

        return new CommentDto(
            comment.Id,
            comment.Text,
            comment.AuthorId,
            comment.ServiceRequestId,
            comment.QuoteId,
            comment.WorkOrderId,
            comment.CreatedAt,
            author.Adapt<UserDto>()
        );
    }
}
