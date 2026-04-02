using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Application.Queries.Invoices;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Invoices;

public record CreateInvoiceCommand(Guid WorkOrderId, CreateInvoiceRequest Request) : IRequest<InvoiceDto>;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    private readonly IRepository<WorkOrder> _workOrders;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IActivityLogger _activityLogger;

    public CreateInvoiceCommandHandler(IRepository<WorkOrder> workOrders, IInvoiceRepository invoiceRepo, IActivityLogger activityLogger)
    {
        _workOrders = workOrders;
        _invoiceRepo = invoiceRepo;
        _activityLogger = activityLogger;
    }

    public async Task<InvoiceDto> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var wo = await _workOrders.Query()
            .Include(w => w.ServiceRequest).ThenInclude(sr => sr.Client).ThenInclude(c => c.User)
            .Include(w => w.Proposal)
            .FirstOrDefaultAsync(w => w.Id == command.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        if (wo.Status != WorkOrderStatus.Completed && wo.Status != WorkOrderStatus.Closed)
            throw new InvalidOperationException("Only completed or closed work orders can be invoiced.");

        var existing = await _invoiceRepo.GetByWorkOrderIdAsync(command.WorkOrderId);
        if (existing != null)
            throw new InvalidOperationException("An invoice already exists for this work order.");

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            WorkOrderId = command.WorkOrderId,
            ClientId = wo.ServiceRequest.ClientId,
            Amount = req.Amount,
            Description = req.Description,
            Notes = req.Notes,
            BillToName = req.BillToName,
            BillToEmail = req.BillToEmail,
            Location = wo.ServiceRequest.Location,
            Status = InvoiceStatus.Draft,
            PublicToken = "inv-" + Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _invoiceRepo.Add(invoice);
        await _invoiceRepo.SaveChangesAsync();

        await _activityLogger.LogAsync(
            wo.ServiceRequestId, wo.Id,
            $"Invoice created for {req.Amount:C}",
            ActivityLogCategory.Financial, string.Empty, null);

        var result = await _invoiceRepo.GetWithDetailsAsync(invoice.Id);
        return InvoiceMappingHelper.MapToDto(result!);
    }
}
