using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.EmailActions;

public record CreateQuoteFromEmailCommand(Guid InboundEmailId) : IRequest<CreateQuoteFromEmailResult>;

public record CreateQuoteFromEmailResult(Guid QuoteId);

public class CreateQuoteFromEmailCommandHandler : IRequestHandler<CreateQuoteFromEmailCommand, CreateQuoteFromEmailResult>
{
    private readonly IRepository<InboundEmail> _inboundEmails;
    private readonly IRepository<Vendor> _vendors;
    private readonly IRepository<Quote> _quotes;
    private readonly IActivityLogger _activityLogger;

    public CreateQuoteFromEmailCommandHandler(
        IRepository<InboundEmail> inboundEmails,
        IRepository<Vendor> vendors,
        IRepository<Quote> quotes,
        IActivityLogger activityLogger)
    {
        _inboundEmails = inboundEmails;
        _vendors = vendors;
        _quotes = quotes;
        _activityLogger = activityLogger;
    }

    public async Task<CreateQuoteFromEmailResult> Handle(CreateQuoteFromEmailCommand command, CancellationToken cancellationToken)
    {
        var email = await _inboundEmails.Query()
            .Include(e => e.ServiceRequest)
            .FirstOrDefaultAsync(e => e.Id == command.InboundEmailId, cancellationToken)
            ?? throw new NotFoundException("Inbound email not found.");

        if (email.ServiceRequestId is null)
            throw new InvalidOperationException("Email is not linked to a service request.");

        var vendor = await _vendors.Query()
            .FirstOrDefaultAsync(v => v.Email == email.FromAddress, cancellationToken);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = email.ServiceRequestId.Value,
            VendorId = vendor?.Id ?? Guid.Empty,
            ScopeOfWork = email.BodyText ?? email.BodyHtml ?? string.Empty,
            Status = QuoteStatus.Submitted,
            PublicToken = "qt-" + Guid.NewGuid().ToString("N"),
            SubmittedAt = DateTime.UtcNow
        };

        _quotes.Add(quote);
        await _quotes.SaveChangesAsync();

        var sender = email.FromName ?? email.FromAddress;
        await _activityLogger.LogAsync(
            email.ServiceRequestId.Value, null,
            $"Created quote from email by {sender}",
            ActivityLogCategory.Financial, string.Empty, null);

        return new CreateQuoteFromEmailResult(quote.Id);
    }
}
