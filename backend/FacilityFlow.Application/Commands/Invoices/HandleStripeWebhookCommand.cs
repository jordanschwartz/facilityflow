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

    public HandleStripeWebhookCommandHandler(IStripeService stripeService, IInvoiceRepository invoiceRepo)
    {
        _stripeService = stripeService;
        _invoiceRepo = invoiceRepo;
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
            }
        }

        return Unit.Value;
    }
}
