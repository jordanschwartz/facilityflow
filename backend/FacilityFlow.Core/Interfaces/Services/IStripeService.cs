namespace FacilityFlow.Core.Interfaces.Services;

public class StripeInvoiceResult
{
    public string InvoiceId { get; init; } = string.Empty;
    public string InvoiceUrl { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
}

public interface IStripeService
{
    Task<StripeInvoiceResult> CreateAndSendInvoiceAsync(string customerEmail, string customerName, string description, decimal amount);
    (bool valid, string? invoiceId, string? eventType) ParseWebhookEvent(string payload, string signature);
}
