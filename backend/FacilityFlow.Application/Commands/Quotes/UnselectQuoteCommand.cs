using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Quotes;

public record UnselectQuoteCommand(Guid ServiceRequestId) : IRequest;

public class UnselectQuoteCommandHandler : IRequestHandler<UnselectQuoteCommand>
{
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IActivityLogger _activityLogger;

    public UnselectQuoteCommandHandler(
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

    public async Task Handle(UnselectQuoteCommand command, CancellationToken cancellationToken)
    {
        var selectedQuote = await _quotes.Query()
            .Include(q => q.Vendor)
            .FirstOrDefaultAsync(q => q.ServiceRequestId == command.ServiceRequestId && q.Status == QuoteStatus.Selected, cancellationToken)
            ?? throw new NotFoundException("No selected quote found.");

        var vendorName = selectedQuote.Vendor?.CompanyName ?? "vendor";

        // Revert quote status to Submitted
        selectedQuote.Status = QuoteStatus.Submitted;

        // Revert vendor invite to QuoteSubmitted
        var invite = await _vendorInvites.Query()
            .FirstOrDefaultAsync(vi => vi.ServiceRequestId == command.ServiceRequestId && vi.VendorId == selectedQuote.VendorId, cancellationToken);
        if (invite != null)
            invite.Status = VendorInviteStatus.QuoteSubmitted;

        // Revert any rejected quotes back to Submitted
        var rejectedQuotes = await _quotes.Query()
            .Where(q => q.ServiceRequestId == command.ServiceRequestId && q.Status == QuoteStatus.Rejected)
            .ToListAsync(cancellationToken);
        foreach (var q in rejectedQuotes)
            q.Status = QuoteStatus.Submitted;

        // Revert rejected invites back to QuoteSubmitted (if they had quotes)
        var rejectedInvites = await _vendorInvites.Query()
            .Where(vi => vi.ServiceRequestId == command.ServiceRequestId && vi.Status == VendorInviteStatus.Rejected)
            .ToListAsync(cancellationToken);
        foreach (var inv in rejectedInvites)
        {
            var hasQuote = await _quotes.Query()
                .AnyAsync(q => q.VendorId == inv.VendorId && q.ServiceRequestId == command.ServiceRequestId, cancellationToken);
            if (hasQuote)
                inv.Status = VendorInviteStatus.QuoteSubmitted;
        }

        // Revert SR status back to PendingQuotes if it was PendingApproval
        var sr = await _serviceRequests.GetByIdAsync(command.ServiceRequestId);
        if (sr != null && sr.Status == ServiceRequestStatus.PendingApproval)
        {
            sr.Status = ServiceRequestStatus.PendingQuotes;
            sr.UpdatedAt = DateTime.UtcNow;
        }

        await _quotes.SaveChangesAsync();

        await _activityLogger.LogAsync(
            command.ServiceRequestId, null,
            $"Unassigned {vendorName}",
            ActivityLogCategory.StatusChange, string.Empty, null);
    }
}
