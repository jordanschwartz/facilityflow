using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.WorkOrders;

public record GetWorkOrderByTokenQuery(string Token) : IRequest<WorkOrderViewDto>;

public class GetWorkOrderByTokenQueryHandler : IRequestHandler<GetWorkOrderByTokenQuery, WorkOrderViewDto>
{
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly IRepository<Quote> _quotes;
    private readonly IActivityLogger _activityLogger;

    public GetWorkOrderByTokenQueryHandler(
        IRepository<VendorInvite> vendorInvites,
        IRepository<Quote> quotes,
        IActivityLogger activityLogger)
    {
        _vendorInvites = vendorInvites;
        _quotes = quotes;
        _activityLogger = activityLogger;
    }

    public async Task<WorkOrderViewDto> Handle(GetWorkOrderByTokenQuery request, CancellationToken cancellationToken)
    {
        var invite = await _vendorInvites.Query()
            .Include(vi => vi.ServiceRequest).ThenInclude(sr => sr.Client)
            .Include(vi => vi.ServiceRequest).ThenInclude(sr => sr.CreatedBy)
            .Include(vi => vi.Vendor)
            .FirstOrDefaultAsync(vi => vi.PublicToken == request.Token, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        // On first view, update status to Viewed
        if (invite.Status == VendorInviteStatus.WorkOrderSent)
        {
            invite.Status = VendorInviteStatus.Viewed;
            await _vendorInvites.SaveChangesAsync();

            await _activityLogger.LogAsync(
                invite.ServiceRequestId, null,
                $"Vendor viewed work order ({invite.Vendor.CompanyName})",
                ActivityLogCategory.Communication, invite.Vendor.CompanyName, null);
        }

        var sr = invite.ServiceRequest;
        var contact = sr.CreatedBy;

        // Look up quote token for this vendor
        var quote = await _quotes.Query()
            .FirstOrDefaultAsync(q => q.ServiceRequestId == sr.Id && q.VendorId == invite.VendorId, cancellationToken);

        var woNum = !string.IsNullOrWhiteSpace(sr.WorkOrderNumber)
            ? sr.WorkOrderNumber
            : $"WO-{sr.Id.ToString("N")[..8].ToUpper()}";

        return new WorkOrderViewDto(
            woNum,
            sr.Title,
            sr.Description,
            sr.Category,
            sr.Priority.ToString(),
            sr.Client.CompanyName,
            sr.Location,
            sr.CreatedAt,
            sr.ScheduledDate,
            $"{contact.FirstName} {contact.LastName}",
            contact.Email,
            quote?.PublicToken,
            invite.Vendor.CompanyName,
            invite.Status.ToString());
    }
}
