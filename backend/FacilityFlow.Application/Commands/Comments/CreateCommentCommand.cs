using FacilityFlow.Application.DTOs.Comments;
using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.Comments;

public record CommentAttachmentInput(Stream Stream, string FileName, string ContentType);

public record CreateCommentCommand(
    string Text,
    Guid UserId,
    Guid? ServiceRequestId,
    Guid? QuoteId,
    Guid? WorkOrderId,
    List<CommentAttachmentInput>? Attachments) : IRequest<CommentDto>;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly IRepository<Comment> _comments;
    private readonly IRepository<User> _users;
    private readonly IRepository<Attachment> _attachments;
    private readonly IFileStorageService _fileStorage;

    public CreateCommentCommandHandler(
        IRepository<Comment> comments, IRepository<User> users,
        IRepository<Attachment> attachments,
        IFileStorageService fileStorage)
    {
        _comments = comments;
        _users = users;
        _attachments = attachments;
        _fileStorage = fileStorage;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand command, CancellationToken cancellationToken)
    {
        var provided = new[] { command.ServiceRequestId.HasValue, command.QuoteId.HasValue, command.WorkOrderId.HasValue }
            .Count(v => v);

        if (provided != 1)
            throw new ArgumentException("Exactly one of serviceRequestId, quoteId, or workOrderId must be provided.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = command.Text,
            AuthorId = command.UserId,
            ServiceRequestId = command.ServiceRequestId,
            QuoteId = command.QuoteId,
            WorkOrderId = command.WorkOrderId,
            CreatedAt = DateTime.UtcNow
        };

        _comments.Add(comment);

        var savedAttachments = new List<Attachment>();
        if (command.Attachments is { Count: > 0 })
        {
            var directory = $"comments/{comment.Id}";
            foreach (var file in command.Attachments)
            {
                var (url, _) = await _fileStorage.SaveFileAsync(directory, file.Stream, file.FileName, file.ContentType);
                var attachment = new Attachment
                {
                    Id = Guid.NewGuid(),
                    CommentId = comment.Id,
                    Filename = file.FileName,
                    MimeType = file.ContentType,
                    Url = url
                };
                _attachments.Add(attachment);
                savedAttachments.Add(attachment);
            }
        }

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
            author.Adapt<UserDto>(),
            savedAttachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList()
        );
    }
}
