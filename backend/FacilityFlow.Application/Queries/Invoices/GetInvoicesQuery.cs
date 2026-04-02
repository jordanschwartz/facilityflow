using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Invoices;

public record GetInvoicesQuery(
    string? Status, Guid? ClientId, string? Location,
    DateTime? DateFrom, DateTime? DateTo,
    int Page, int PageSize) : IRequest<PagedResult<InvoiceSummaryDto>>;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    private readonly IInvoiceRepository _invoiceRepo;

    public GetInvoicesQueryHandler(IInvoiceRepository invoiceRepo) => _invoiceRepo = invoiceRepo;

    public async Task<PagedResult<InvoiceSummaryDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _invoiceRepo.Query()
            .Include(i => i.WorkOrder)
            .Include(i => i.Client)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<InvoiceStatus>(request.Status, true, out var parsedStatus))
            query = query.Where(i => i.Status == parsedStatus);

        if (request.ClientId.HasValue)
            query = query.Where(i => i.ClientId == request.ClientId.Value);

        if (!string.IsNullOrWhiteSpace(request.Location))
            query = query.Where(i => i.Location.Contains(request.Location));

        if (request.DateFrom.HasValue)
            query = query.Where(i => i.CreatedAt >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(i => i.CreatedAt <= request.DateTo.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(i => new InvoiceSummaryDto(
            i.Id, i.WorkOrderId,
            i.Client.CompanyName, i.Location,
            i.WorkOrder.CompletedAt, i.Amount,
            i.Status.ToString(), i.SentAt, i.PaidAt,
            i.BillToEmail)).ToList();

        return new PagedResult<InvoiceSummaryDto>(dtos, total, request.Page, request.PageSize);
    }
}
