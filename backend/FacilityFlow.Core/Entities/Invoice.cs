using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid ClientId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string BillToName { get; set; } = string.Empty;
    public string BillToEmail { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string PublicToken { get; set; } = string.Empty;
    public string? StripeInvoiceId { get; set; }
    public string? StripeInvoiceUrl { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WorkOrder WorkOrder { get; set; } = null!;
    public Client Client { get; set; } = null!;
}
