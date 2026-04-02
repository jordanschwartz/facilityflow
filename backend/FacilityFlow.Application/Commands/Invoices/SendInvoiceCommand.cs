using FacilityFlow.Application.DTOs.Invoices;
using FacilityFlow.Application.Queries.Invoices;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;

namespace FacilityFlow.Application.Commands.Invoices;

public record SendInvoiceCommand(Guid Id) : IRequest<InvoiceDto>;

public class SendInvoiceCommandHandler : IRequestHandler<SendInvoiceCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IStripeService _stripeService;
    private readonly INotificationService _notifications;

    public SendInvoiceCommandHandler(
        IInvoiceRepository invoiceRepo,
        IStripeService stripeService,
        INotificationService notifications)
    {
        _invoiceRepo = invoiceRepo;
        _stripeService = stripeService;
        _notifications = notifications;
    }

    public async Task<InvoiceDto> Handle(SendInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepo.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be sent.");

        var result = await _stripeService.CreateAndSendInvoiceAsync(
            invoice.BillToEmail, invoice.BillToName, invoice.Description, invoice.Amount);

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTime.UtcNow;
        invoice.StripeInvoiceId = result.InvoiceId;
        invoice.StripeInvoiceUrl = result.InvoiceUrl;
        invoice.StripeCustomerId = result.CustomerId;

        await _invoiceRepo.SaveChangesAsync();

        await _notifications.CreateAsync(
            invoice.Client.UserId,
            "Invoice.Sent",
            $"An invoice for {invoice.Amount:C} has been sent for: {invoice.WorkOrder?.ServiceRequest?.Title}",
            "/invoices");

        var reloaded = await _invoiceRepo.GetWithDetailsAsync(command.Id);
        return InvoiceMappingHelper.MapToDto(reloaded!);
    }
}
