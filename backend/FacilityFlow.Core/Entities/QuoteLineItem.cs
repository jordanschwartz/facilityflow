namespace FacilityFlow.Core.Entities;

public class QuoteLineItem
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    // Total is computed: Quantity * UnitPrice
    public Quote Quote { get; set; } = null!;
}
