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

public record ForwardOutboundEmailCommand(Guid OutboundEmailId, string RecipientEmail, string? RecipientName) : IRequest;

public class ForwardOutboundEmailCommandHandler : IRequestHandler<ForwardOutboundEmailCommand>
{
    private readonly IRepository<OutboundEmail> _outboundEmails;
    private readonly IEmailService _emailService;
    private readonly IActivityLogger _activityLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardOutboundEmailCommandHandler(
        IRepository<OutboundEmail> outboundEmails,
        IEmailService emailService,
        IActivityLogger activityLogger,
        IHttpContextAccessor httpContextAccessor)
    {
        _outboundEmails = outboundEmails;
        _emailService = emailService;
        _activityLogger = activityLogger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Handle(ForwardOutboundEmailCommand command, CancellationToken cancellationToken)
    {
        var original = await _outboundEmails.Query()
            .FirstOrDefaultAsync(e => e.Id == command.OutboundEmailId, cancellationToken)
            ?? throw new NotFoundException("Outbound email not found.");

        var subject = original.Subject.StartsWith("Fwd: ", StringComparison.OrdinalIgnoreCase)
            ? original.Subject
            : $"Fwd: {original.Subject}";

        await _emailService.SendEmailAsync(command.RecipientEmail, subject, original.BodyHtml);

        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value ?? "System";
        Guid.TryParse(userIdClaim, out var sentById);

        var forwarded = new OutboundEmail
        {
            ServiceRequestId = original.ServiceRequestId,
            RecipientAddress = command.RecipientEmail,
            RecipientName = command.RecipientName,
            Subject = subject,
            BodyHtml = original.BodyHtml,
            SentAt = DateTime.UtcNow,
            SentById = sentById,
            SentByName = userName,
            EmailType = original.EmailType,
            ConversationId = original.ConversationId
        };

        _outboundEmails.Add(forwarded);
        await _outboundEmails.SaveChangesAsync();

        await _activityLogger.LogAsync(
            original.ServiceRequestId, null,
            $"Forwarded email to {command.RecipientEmail}",
            ActivityLogCategory.Communication, string.Empty, null);
    }
}
