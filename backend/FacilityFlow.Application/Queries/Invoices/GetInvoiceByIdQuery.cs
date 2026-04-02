using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Invoices;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto>;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepo;

    public GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepo) => _invoiceRepo = invoiceRepo;

    public async Task<InvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepo.GetWithDetailsAsync(request.Id)
            ?? throw new NotFoundException("Invoice not found.");

        return InvoiceMappingHelper.MapToDto(invoice);
    }
}

internal static class InvoiceMappingHelper
{
    internal static InvoiceDto MapToDto(Invoice i) => new(
        i.Id, i.WorkOrderId, i.ClientId,
        i.Amount, i.Description, i.Notes,
        i.BillToName, i.BillToEmail, i.Location,
        i.Status.ToString(), i.PublicToken,
        i.StripeInvoiceUrl,
        i.SentAt, i.PaidAt, i.CreatedAt,
        i.WorkOrder?.ServiceRequest?.Title,
        i.WorkOrder?.Vendor?.CompanyName,
        i.Client != null ? new ClientSummaryDto(i.Client.Id, i.Client.CompanyName, i.Client.Phone) : null);
}
