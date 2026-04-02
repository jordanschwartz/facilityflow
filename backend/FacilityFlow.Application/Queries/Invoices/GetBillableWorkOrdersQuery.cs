using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Invoices;

public record GetBillableWorkOrdersQuery(int Page, int PageSize) : IRequest<PagedResult<BillableWorkOrderDto>>;

public class GetBillableWorkOrdersQueryHandler : IRequestHandler<GetBillableWorkOrdersQuery, PagedResult<BillableWorkOrderDto>>
{
    private readonly IRepository<WorkOrder> _workOrders;
    private readonly IRepository<Invoice> _invoices;

    public GetBillableWorkOrdersQueryHandler(IRepository<WorkOrder> workOrders, IRepository<Invoice> invoices)
    {
        _workOrders = workOrders;
        _invoices = invoices;
    }

    public async Task<PagedResult<BillableWorkOrderDto>> Handle(GetBillableWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var invoicedWoIds = _invoices.Query().Select(i => i.WorkOrderId);

        var query = _workOrders.Query()
            .Include(wo => wo.ServiceRequest).ThenInclude(sr => sr.Client).ThenInclude(c => c.User)
            .Include(wo => wo.Proposal)
            .Where(wo => wo.Status == WorkOrderStatus.Completed || wo.Status == WorkOrderStatus.Closed)
            .Where(wo => !invoicedWoIds.Contains(wo.Id));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(wo => wo.CompletedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(wo => new BillableWorkOrderDto(
            wo.Id,
            wo.ServiceRequestId,
            wo.ServiceRequest.Title,
            wo.ServiceRequest.Location,
            wo.ServiceRequest.Client.CompanyName,
            wo.ServiceRequest.Client.User.Email,
            wo.CompletedAt,
            wo.Proposal?.Price,
            wo.Proposal?.ScopeOfWork)).ToList();

        return new PagedResult<BillableWorkOrderDto>(dtos, total, request.Page, request.PageSize);
    }
}
