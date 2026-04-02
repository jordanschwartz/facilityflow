using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Invoices;

public record HandleStripeWebhookCommand(string Payload, string Signature) : IRequest<Unit>;

public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand, Unit>
{
    private readonly IStripeService _stripeService;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IActivityLogger _activityLogger;

    public HandleStripeWebhookCommandHandler(IStripeService stripeService, IInvoiceRepository invoiceRepo, IActivityLogger activityLogger)
    {
        _stripeService = stripeService;
        _invoiceRepo = invoiceRepo;
        _activityLogger = activityLogger;
    }

    public async Task<Unit> Handle(HandleStripeWebhookCommand command, CancellationToken cancellationToken)
    {
        var (valid, invoiceId, eventType) = _stripeService.ParseWebhookEvent(command.Payload, command.Signature);

        if (!valid)
            throw new InvalidOperationException("Invalid webhook signature");

        if (eventType == "invoice.paid" && invoiceId != null)
        {
            var invoice = await _invoiceRepo.Query()
                .FirstOrDefaultAsync(i => i.StripeInvoiceId == invoiceId, cancellationToken);

            if (invoice != null && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
                await _invoiceRepo.SaveChangesAsync();

                var fullInvoice = await _invoiceRepo.GetWithDetailsAsync(invoice.Id);
                if (fullInvoice?.WorkOrder != null)
                {
                    await _activityLogger.LogAsync(
                        fullInvoice.WorkOrder.ServiceRequestId, fullInvoice.WorkOrderId,
                        $"Invoice marked as paid ({fullInvoice.Amount:C})",
                        ActivityLogCategory.Financial, "System", null);
                }
            }
        }

        return Unit.Value;
    }
}
