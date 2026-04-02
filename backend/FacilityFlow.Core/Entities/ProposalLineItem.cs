namespace FacilityFlow.Core.Entities;

public class ProposalLineItem
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
    public Proposal Proposal { get; set; } = null!;
}
