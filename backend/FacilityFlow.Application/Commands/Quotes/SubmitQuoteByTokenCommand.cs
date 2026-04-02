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

public record SubmitQuoteByTokenCommand(string Token, SubmitQuoteRequest Request) : IRequest<QuoteDto>;

public class SubmitQuoteByTokenCommandHandler : IRequestHandler<SubmitQuoteByTokenCommand, QuoteDto>
{
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;

    public SubmitQuoteByTokenCommandHandler(
        IQuoteRepository quotes,
        IRepository<VendorInvite> vendorInvites,
        INotificationService notifications,
        IUserRepository users)
    {
        _quotes = quotes;
        _vendorInvites = vendorInvites;
        _notifications = notifications;
        _users = users;
    }

    public async Task<QuoteDto> Handle(SubmitQuoteByTokenCommand command, CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetByTokenWithDetailsAsync(command.Token)
            ?? throw new NotFoundException("Quote not found.");

        if (quote.Status != QuoteStatus.Requested)
            throw new InvalidOperationException("Quote has already been submitted or is no longer accepting responses.");

        var req = command.Request;
        quote.Price = req.Price;
        quote.ScopeOfWork = req.ScopeOfWork;
        quote.ProposedStartDate = req.ProposedStartDate;
        quote.EstimatedDurationValue = req.EstimatedDurationValue;
        quote.EstimatedDurationUnit = req.EstimatedDurationUnit;
        quote.NotToExceedPrice = req.NotToExceedPrice;
        quote.Assumptions = req.Assumptions;
        quote.Exclusions = req.Exclusions;
        quote.VendorAvailability = req.VendorAvailability;
        quote.ValidUntil = req.ValidUntil;
        quote.Status = QuoteStatus.Submitted;
        quote.SubmittedAt = DateTime.UtcNow;

        // Replace line items
        quote.LineItems.Clear();
        if (req.LineItems != null)
        {
            foreach (var li in req.LineItems)
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

        // Update vendor invite status
        var invite = await _vendorInvites.Query()
            .FirstOrDefaultAsync(vi => vi.ServiceRequestId == quote.ServiceRequestId && vi.VendorId == quote.VendorId, cancellationToken);
        if (invite != null)
            invite.Status = VendorInviteStatus.Quoted;

        // Transition SR to Quoting if in Sourcing
        if (quote.ServiceRequest.Status == ServiceRequestStatus.Sourcing)
        {
            quote.ServiceRequest.Status = ServiceRequestStatus.Quoting;
            quote.ServiceRequest.UpdatedAt = DateTime.UtcNow;
        }

        await _quotes.SaveChangesAsync();

        // Notify operators
        var operators = await _users.GetByRoleAsync(UserRole.Operator);
        foreach (var op in operators)
        {
            await _notifications.CreateAsync(op.Id, "Quote.Submitted",
                $"A new quote has been submitted for service request: {quote.ServiceRequest.Title}",
                $"/service-requests/{quote.ServiceRequestId}");
        }

        return QuoteMappingHelper.MapToDto(quote);
    }
}
