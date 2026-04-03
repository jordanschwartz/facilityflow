using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Quotes;

public record ManualQuoteEntryCommand(
    Guid ServiceRequestId,
    Guid VendorInviteId,
    decimal Price,
    string ScopeOfWork,
    DateTime? ProposedStartDate = null,
    int? EstimatedDurationValue = null,
    string? EstimatedDurationUnit = null,
    decimal? NotToExceedPrice = null,
    string? Assumptions = null,
    string? Exclusions = null,
    string? VendorAvailability = null,
    DateTime? ValidUntil = null,
    List<ManualQuoteLineItem>? LineItems = null
) : IRequest<Guid>;

public record ManualQuoteLineItem(string Description, decimal Quantity, decimal UnitPrice);

public class ManualQuoteEntryCommandHandler : IRequestHandler<ManualQuoteEntryCommand, Guid>
{
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IActivityLogger _activityLogger;

    public ManualQuoteEntryCommandHandler(
        IQuoteRepository quotes,
        IRepository<VendorInvite> vendorInvites,
        IRepository<ServiceRequest> serviceRequests,
        IActivityLogger activityLogger)
    {
        _quotes = quotes;
        _vendorInvites = vendorInvites;
        _serviceRequests = serviceRequests;
        _activityLogger = activityLogger;
    }

    public async Task<Guid> Handle(ManualQuoteEntryCommand command, CancellationToken cancellationToken)
    {
        // 1. Look up VendorInvite (include Vendor) by VendorInviteId + ServiceRequestId
        var invite = await _vendorInvites.Query()
            .Include(vi => vi.Vendor)
            .Include(vi => vi.ServiceRequest)
            .FirstOrDefaultAsync(vi => vi.Id == command.VendorInviteId && vi.ServiceRequestId == command.ServiceRequestId, cancellationToken)
            ?? throw new NotFoundException("Vendor invite not found.");

        // 2. Allow from any status EXCEPT Rejected
        if (invite.Status == VendorInviteStatus.Rejected)
            throw new InvalidOperationException("Cannot enter a quote for a rejected vendor invite.");

        // 3. Check if a Quote already exists for this vendor + service request
        var existingQuote = await _quotes.Query()
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.VendorId == invite.VendorId && q.ServiceRequestId == command.ServiceRequestId, cancellationToken);

        Quote quote;

        if (existingQuote != null)
        {
            if (existingQuote.Status != QuoteStatus.Requested)
                throw new InvalidOperationException("Quote already submitted for this vendor.");

            // Update existing quote with new data
            quote = existingQuote;
        }
        else
        {
            // Create new Quote record
            quote = new Quote
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = command.ServiceRequestId,
                VendorId = invite.VendorId,
                PublicToken = "qt-" + Guid.NewGuid().ToString("N"),
            };
            _quotes.Add(quote);
        }

        // 4. Set quote fields
        quote.Price = command.Price;
        quote.ScopeOfWork = command.ScopeOfWork;
        quote.ProposedStartDate = command.ProposedStartDate.HasValue
            ? DateTime.SpecifyKind(command.ProposedStartDate.Value, DateTimeKind.Utc) : null;
        quote.EstimatedDurationValue = command.EstimatedDurationValue;
        quote.EstimatedDurationUnit = command.EstimatedDurationUnit;
        quote.NotToExceedPrice = command.NotToExceedPrice;
        quote.Assumptions = command.Assumptions;
        quote.Exclusions = command.Exclusions;
        quote.VendorAvailability = command.VendorAvailability;
        quote.ValidUntil = command.ValidUntil.HasValue
            ? DateTime.SpecifyKind(command.ValidUntil.Value, DateTimeKind.Utc) : null;
        quote.Status = QuoteStatus.Submitted;
        quote.SubmittedAt = DateTime.UtcNow;

        // 7. Handle line items: clear existing and add new ones if provided
        quote.LineItems.Clear();
        if (command.LineItems != null)
        {
            foreach (var li in command.LineItems)
            {
                quote.LineItems.Add(new QuoteLineItem
                {
                    Id = Guid.NewGuid(),
                    QuoteId = quote.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice
                });
            }
        }

        // 5. Update VendorInvite status to QuoteSubmitted
        invite.Status = VendorInviteStatus.QuoteSubmitted;

        // 6. If ServiceRequest is in New/Qualifying/Sourcing status, transition to PendingQuotes
        var sr = invite.ServiceRequest;
        if (sr.Status is ServiceRequestStatus.New or ServiceRequestStatus.Qualifying or ServiceRequestStatus.Sourcing)
        {
            sr.Status = ServiceRequestStatus.PendingQuotes;
            sr.UpdatedAt = DateTime.UtcNow;
        }

        await _quotes.SaveChangesAsync();

        // 8. Log activity
        var vendorName = invite.Vendor?.CompanyName ?? "vendor";
        await _activityLogger.LogAsync(
            command.ServiceRequestId, null,
            $"Manually entered quote for {vendorName} — ${command.Price}",
            ActivityLogCategory.Communication, vendorName, null);

        // 9. Return the Quote Id
        return quote.Id;
    }
}
