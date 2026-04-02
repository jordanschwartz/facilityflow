using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.WorkOrders;

public record GetWorkOrderByIdQuery(Guid Id) : IRequest<WorkOrderDto>;

public class GetWorkOrderByIdQueryHandler : IRequestHandler<GetWorkOrderByIdQuery, WorkOrderDto>
{
    private readonly IRepository<WorkOrder> _repo;

    public GetWorkOrderByIdQueryHandler(IRepository<WorkOrder> repo) => _repo = repo;

    public async Task<WorkOrderDto> Handle(GetWorkOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var wo = await _repo.Query()
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Proposal)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(w => w.Vendor)
            .Include(w => w.Attachments)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        return MapToDetail(wo);
    }

    private static ServiceRequestSummaryDto MapSrToSummary(ServiceRequest sr) =>
        new(
            sr.Id,
            sr.Title,
            sr.Priority.ToString(),
            sr.Status.ToString(),
            sr.ClientId,
            sr.CreatedAt,
            sr.UpdatedAt,
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null
        );

    private static WorkOrderDto MapToDetail(WorkOrder wo) =>
        new(
            wo.Id,
            wo.ServiceRequestId,
            wo.ProposalId,
            wo.VendorId,
            wo.Status.ToString(),
            wo.VendorNotes,
            wo.CompletedAt,
            MapSrToSummary(wo.ServiceRequest),
            new VendorSummaryDto(wo.Vendor.Id, wo.Vendor.CompanyName, wo.Vendor.Trades, wo.Vendor.Rating),
            wo.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList()
        );
}
