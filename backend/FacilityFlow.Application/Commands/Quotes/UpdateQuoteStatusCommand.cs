using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Application.Queries.Quotes;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Quotes;

public record UpdateQuoteStatusCommand(Guid Id, UpdateQuoteStatusRequest Request) : IRequest<QuoteDto>;

public class UpdateQuoteStatusCommandHandler : IRequestHandler<UpdateQuoteStatusCommand, QuoteDto>
{
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IActivityLogger _activityLogger;

    public UpdateQuoteStatusCommandHandler(IQuoteRepository quotes, IRepository<ServiceRequest> serviceRequests, IActivityLogger activityLogger)
    {
        _quotes = quotes;
        _serviceRequests = serviceRequests;
        _activityLogger = activityLogger;
    }

    public async Task<QuoteDto> Handle(UpdateQuoteStatusCommand command, CancellationToken cancellationToken)
    {
        var quote = await _quotes.Query()
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Quote not found.");

        if (command.Request.Status == QuoteStatus.Selected)
        {
            // Auto-reject all other quotes for this SR
            var otherQuotes = await _quotes.Query()
                .Where(q => q.ServiceRequestId == quote.ServiceRequestId && q.Id != command.Id && q.Status != QuoteStatus.Rejected)
                .ToListAsync(cancellationToken);
            foreach (var other in otherQuotes)
                other.Status = QuoteStatus.Rejected;

            // Transition SR to PendingApproval
            var sr = await _serviceRequests.GetByIdAsync(quote.ServiceRequestId);
            if (sr != null && sr.Status == ServiceRequestStatus.PendingQuotes)
            {
                sr.Status = ServiceRequestStatus.PendingApproval;
                sr.UpdatedAt = DateTime.UtcNow;
            }
        }

        quote.Status = command.Request.Status;
        await _quotes.SaveChangesAsync();

        var action = command.Request.Status == QuoteStatus.Selected
            ? $"Selected quote from {quote.Vendor?.CompanyName ?? "vendor"}"
            : $"Rejected quote from {quote.Vendor?.CompanyName ?? "vendor"}";
        await _activityLogger.LogAsync(
            quote.ServiceRequestId, null,
            action,
            ActivityLogCategory.StatusChange, string.Empty, null);

        return QuoteMappingHelper.MapToDto(quote);
    }
}
