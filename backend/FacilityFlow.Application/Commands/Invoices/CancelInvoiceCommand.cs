using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Application.Queries.Invoices;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;

namespace FacilityFlow.Application.Commands.Invoices;

public record CancelInvoiceCommand(Guid Id) : IRequest<InvoiceDto>;

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IActivityLogger _activityLogger;

    public CancelInvoiceCommandHandler(IInvoiceRepository invoiceRepo, IActivityLogger activityLogger)
    {
        _invoiceRepo = invoiceRepo;
        _activityLogger = activityLogger;
    }

    public async Task<InvoiceDto> Handle(CancelInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepo.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Invoice not found.");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Paid invoices cannot be cancelled.");

        invoice.Status = InvoiceStatus.Cancelled;
        await _invoiceRepo.SaveChangesAsync();

        if (invoice.WorkOrder != null)
        {
            await _activityLogger.LogAsync(
                invoice.WorkOrder.ServiceRequestId, invoice.WorkOrderId,
                "Invoice cancelled",
                ActivityLogCategory.Financial, string.Empty, null);
        }

        return InvoiceMappingHelper.MapToDto(invoice);
    }
}
