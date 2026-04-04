using System.Security.Claims;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.EmailActions;

public record AddEmailToNotesCommand(Guid InboundEmailId) : IRequest;

public class AddEmailToNotesCommandHandler : IRequestHandler<AddEmailToNotesCommand>
{
    private readonly IRepository<InboundEmail> _inboundEmails;
    private readonly IRepository<Comment> _comments;
    private readonly IActivityLogger _activityLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AddEmailToNotesCommandHandler(
        IRepository<InboundEmail> inboundEmails,
        IRepository<Comment> comments,
        IActivityLogger activityLogger,
        IHttpContextAccessor httpContextAccessor)
    {
        _inboundEmails = inboundEmails;
        _comments = comments;
        _activityLogger = activityLogger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Handle(AddEmailToNotesCommand command, CancellationToken cancellationToken)
    {
        var email = await _inboundEmails.Query()
            .Include(e => e.ServiceRequest)
            .FirstOrDefaultAsync(e => e.Id == command.InboundEmailId, cancellationToken)
            ?? throw new NotFoundException("Inbound email not found.");

        if (email.ServiceRequestId is null)
            throw new InvalidOperationException("Email is not linked to a service request.");

        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(userIdClaim, out var authorId);

        var sender = email.FromName ?? email.FromAddress;
        var attribution = $"From email by {sender} on {email.ReceivedAt:yyyy-MM-dd HH:mm UTC}";
        var body = email.BodyText ?? email.BodyHtml ?? string.Empty;
        var text = $"{attribution}\n\n{body}";

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Text = text,
            AuthorId = authorId,
            ServiceRequestId = email.ServiceRequestId.Value,
            CreatedAt = DateTime.UtcNow
        };

        _comments.Add(comment);
        await _comments.SaveChangesAsync();

        await _activityLogger.LogAsync(
            email.ServiceRequestId.Value, null,
            "Added email content to notes",
            ActivityLogCategory.Note, string.Empty, null);
    }
}
