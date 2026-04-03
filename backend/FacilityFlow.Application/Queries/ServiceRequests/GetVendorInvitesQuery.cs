using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.VendorInvites;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.ServiceRequests;

public record GetVendorInvitesQuery(Guid ServiceRequestId) : IRequest<List<VendorInviteDto>>;

public class GetVendorInvitesQueryHandler : IRequestHandler<GetVendorInvitesQuery, List<VendorInviteDto>>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly IRepository<Quote> _quotes;

    public GetVendorInvitesQueryHandler(
        IServiceRequestRepository serviceRequests,
        IRepository<VendorInvite> vendorInvites,
        IRepository<Quote> quotes)
    {
        _serviceRequests = serviceRequests;
        _vendorInvites = vendorInvites;
        _quotes = quotes;
    }

    public async Task<List<VendorInviteDto>> Handle(GetVendorInvitesQuery request, CancellationToken cancellationToken)
    {
        if (!await _serviceRequests.ExistsAsync(request.ServiceRequestId))
            throw new NotFoundException("Service request not found.");

        var invites = await _vendorInvites.Query()
            .Include(vi => vi.Vendor).ThenInclude(v => v.User)
            .Where(vi => vi.ServiceRequestId == request.ServiceRequestId)
            .ToListAsync(cancellationToken);

        var dtos = new List<VendorInviteDto>();
        foreach (var inv in invites)
        {
            var q = await _quotes.Query()
                .FirstOrDefaultAsync(qt => qt.ServiceRequestId == request.ServiceRequestId && qt.VendorId == inv.VendorId, cancellationToken);

            dtos.Add(new VendorInviteDto(
                inv.Id,
                inv.ServiceRequestId,
                inv.VendorId,
                inv.Status.ToString(),
                inv.SentAt,
                new VendorSummaryDto(inv.Vendor.Id, inv.Vendor.CompanyName, inv.Vendor.Trades, inv.Vendor.Rating),
                q == null ? null : new QuoteSummaryDto(q.Id, q.Status.ToString(), q.Price == 0 ? null : q.Price, q.SubmittedAt),
                inv.PublicToken
            ));
        }

        return dtos;
    }
}
