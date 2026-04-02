using FacilityFlow.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace FacilityFlow.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly string _secretKey;
    private readonly string _webhookSecret;

    public StripeService(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
    }

    public async Task<StripeInvoiceResult> CreateAndSendInvoiceAsync(
        string customerEmail, string customerName, string description, decimal amount)
    {
        StripeConfiguration.ApiKey = _secretKey;

        // Find or create customer
        var customerService = new CustomerService();
        var customers = await customerService.ListAsync(new CustomerListOptions { Email = customerEmail, Limit = 1 });
        var customer = customers.Data.FirstOrDefault();

        if (customer == null)
        {
            customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = customerEmail,
                Name = customerName
            });
        }

        // Create invoice first (as draft)
        var invoiceService = new Stripe.InvoiceService();
        var invoice = await invoiceService.CreateAsync(new InvoiceCreateOptions
        {
            Customer = customer.Id,
            AutoAdvance = true,
            CollectionMethod = "send_invoice",
            DaysUntilDue = 30
        });

        // Create invoice item attached to the specific invoice
        var invoiceItemService = new InvoiceItemService();
        await invoiceItemService.CreateAsync(new InvoiceItemCreateOptions
        {
            Customer = customer.Id,
            Invoice = invoice.Id,
            Amount = (long)(amount * 100),
            Currency = "usd",
            Description = description
        });

        // Finalize — with auto_advance=true, Stripe will send the invoice automatically
        invoice = await invoiceService.FinalizeInvoiceAsync(invoice.Id);

        return new StripeInvoiceResult
        {
            InvoiceId = invoice.Id,
            InvoiceUrl = invoice.HostedInvoiceUrl ?? string.Empty,
            CustomerId = customer.Id
        };
    }

    public (bool valid, string? invoiceId, string? eventType) ParseWebhookEvent(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);

            if (stripeEvent.Type == "invoice.paid")
            {
                var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                return (true, invoice?.Id, "invoice.paid");
            }

            return (true, null, stripeEvent.Type);
        }
        catch
        {
            return (false, null, null);
        }
    }
}
