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

public record ResendOutboundEmailCommand(Guid OutboundEmailId) : IRequest;

public class ResendOutboundEmailCommandHandler : IRequestHandler<ResendOutboundEmailCommand>
{
    private readonly IRepository<OutboundEmail> _outboundEmails;
    private readonly IEmailService _emailService;
    private readonly IActivityLogger _activityLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResendOutboundEmailCommandHandler(
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

    public async Task Handle(ResendOutboundEmailCommand command, CancellationToken cancellationToken)
    {
        var original = await _outboundEmails.Query()
            .FirstOrDefaultAsync(e => e.Id == command.OutboundEmailId, cancellationToken)
            ?? throw new NotFoundException("Outbound email not found.");

        await _emailService.SendEmailAsync(original.RecipientAddress, original.Subject, original.BodyHtml);

        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value ?? "System";
        Guid.TryParse(userIdClaim, out var sentById);

        var resent = new OutboundEmail
        {
            ServiceRequestId = original.ServiceRequestId,
            RecipientAddress = original.RecipientAddress,
            RecipientName = original.RecipientName,
            Subject = original.Subject,
            BodyHtml = original.BodyHtml,
            SentAt = DateTime.UtcNow,
            SentById = sentById,
            SentByName = userName,
            EmailType = original.EmailType,
            ConversationId = original.ConversationId
        };

        _outboundEmails.Add(resent);
        await _outboundEmails.SaveChangesAsync();

        var recipient = original.RecipientName ?? original.RecipientAddress;
        await _activityLogger.LogAsync(
            original.ServiceRequestId, null,
            $"Re-sent email to {recipient}",
            ActivityLogCategory.Communication, string.Empty, null);
    }
}
