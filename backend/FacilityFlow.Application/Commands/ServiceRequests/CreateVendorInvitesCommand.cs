using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.VendorInvites;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record CreateVendorInvitesCommand(Guid ServiceRequestId, CreateVendorInvitesRequest Request) : IRequest<CreateVendorInvitesResponse>;

public class CreateVendorInvitesCommandHandler : IRequestHandler<CreateVendorInvitesCommand, CreateVendorInvitesResponse>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IRepository<Vendor> _vendors;
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly IRepository<Quote> _quotes;
    private readonly INotificationService _notifications;

    public CreateVendorInvitesCommandHandler(
        IServiceRequestRepository serviceRequests,
        IRepository<Vendor> vendors,
        IRepository<VendorInvite> vendorInvites,
        IRepository<Quote> quotes,
        INotificationService notifications)
    {
        _serviceRequests = serviceRequests;
        _vendors = vendors;
        _vendorInvites = vendorInvites;
        _quotes = quotes;
        _notifications = notifications;
    }

    public async Task<CreateVendorInvitesResponse> Handle(CreateVendorInvitesCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithInvitesAsync(command.ServiceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var existingVendorIds = sr.VendorInvites.Select(vi => vi.VendorId).ToHashSet();
        var created = new List<VendorInvite>();
        var skipped = new List<Guid>();

        foreach (var vendorId in command.Request.VendorIds)
        {
            if (existingVendorIds.Contains(vendorId))
            {
                skipped.Add(vendorId);
                continue;
            }

            var vendor = await _vendors.GetByIdAsync(vendorId);
            if (vendor == null)
            {
                skipped.Add(vendorId);
                continue;
            }

            var invite = new VendorInvite
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = command.ServiceRequestId,
                VendorId = vendorId,
                Status = VendorInviteStatus.Invited,
                SentAt = DateTime.UtcNow
            };

            var quote = new Quote
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = command.ServiceRequestId,
                VendorId = vendorId,
                Price = 0m,
                ScopeOfWork = string.Empty,
                Status = QuoteStatus.Requested,
                PublicToken = "qt-" + Guid.NewGuid().ToString("N")
            };

            _vendorInvites.Add(invite);
            _quotes.Add(quote);
            created.Add(invite);

            if (vendor.UserId.HasValue)
                await _notifications.CreateAsync(vendor.UserId.Value, "VendorInvite.Received",
                    $"You have been invited to quote on service request: {sr.Title}",
                    $"/quotes/submit/{quote.PublicToken}");
        }

        if (created.Any() && sr.Status == ServiceRequestStatus.New)
        {
            sr.Status = ServiceRequestStatus.Sourcing;
            sr.UpdatedAt = DateTime.UtcNow;
        }

        await _serviceRequests.SaveChangesAsync();

        var createdDtos = new List<VendorInviteDto>();
        foreach (var inv in created)
        {
            var v = await _vendors.Query()
                .Include(vn => vn.User)
                .FirstOrDefaultAsync(vn => vn.Id == inv.VendorId, cancellationToken);

            var q = await _quotes.Query()
                .FirstOrDefaultAsync(qt => qt.ServiceRequestId == command.ServiceRequestId && qt.VendorId == inv.VendorId, cancellationToken);

            createdDtos.Add(new VendorInviteDto(
                inv.Id,
                inv.ServiceRequestId,
                inv.VendorId,
                inv.Status.ToString(),
                inv.SentAt,
                new VendorSummaryDto(v!.Id, v.CompanyName, v.Trades, v.Rating),
                q == null ? null : new QuoteSummaryDto(q.Id, q.Status.ToString(), q.Price == 0 ? null : q.Price, q.SubmittedAt)
            ));
        }

        return new CreateVendorInvitesResponse(createdDtos, skipped);
    }
}
