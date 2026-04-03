using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.WorkOrders;

public record GetWorkOrdersQuery(
    string? Status,
    Guid? VendorId,
    int Page,
    int PageSize) : IRequest<PagedResult<WorkOrderSummaryDto>>;

public class GetWorkOrdersQueryHandler : IRequestHandler<GetWorkOrdersQuery, PagedResult<WorkOrderSummaryDto>>
{
    private readonly IRepository<WorkOrder> _repo;

    public GetWorkOrdersQueryHandler(IRepository<WorkOrder> repo) => _repo = repo;

    public async Task<PagedResult<WorkOrderSummaryDto>> Handle(GetWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query()
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.Proposal)
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(wo => wo.Vendor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<WorkOrderStatus>(request.Status, true, out var parsedStatus))
            query = query.Where(wo => wo.Status == parsedStatus);

        if (request.VendorId.HasValue)
            query = query.Where(wo => wo.VendorId == request.VendorId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(wo => wo.ServiceRequest.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToSummary).ToList();
        return new PagedResult<WorkOrderSummaryDto>(dtos, total, request.Page, request.PageSize);
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
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone, sr.Client.WorkOrderPrefix),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null,
            sr.WorkOrderNumber
        );

    private static WorkOrderSummaryDto MapToSummary(WorkOrder wo) =>
        new(
            wo.Id,
            wo.ServiceRequestId,
            wo.Status.ToString(),
            wo.CompletedAt,
            MapSrToSummary(wo.ServiceRequest),
            new VendorSummaryDto(wo.Vendor.Id, wo.Vendor.CompanyName, wo.Vendor.Trades, wo.Vendor.Rating)
        );
}
