using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Application.Queries.Invoices;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Invoices;

public record UpdateInvoiceCommand(Guid Id, UpdateInvoiceRequest Request) : IRequest<InvoiceDto>;

public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepo;

    public UpdateInvoiceCommandHandler(IInvoiceRepository invoiceRepo) => _invoiceRepo = invoiceRepo;

    public async Task<InvoiceDto> Handle(UpdateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(command.Id)
            ?? throw new NotFoundException("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be updated.");

        var req = command.Request;

        if (req.Amount.HasValue) invoice.Amount = req.Amount.Value;
        if (req.Description != null) invoice.Description = req.Description;
        if (req.BillToName != null) invoice.BillToName = req.BillToName;
        if (req.BillToEmail != null) invoice.BillToEmail = req.BillToEmail;
        if (req.Notes != null) invoice.Notes = req.Notes;

        await _invoiceRepo.SaveChangesAsync();

        var result = await _invoiceRepo.GetWithDetailsAsync(command.Id);
        return InvoiceMappingHelper.MapToDto(result!);
    }
}
