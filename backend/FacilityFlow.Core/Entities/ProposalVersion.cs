namespace FacilityFlow.Core.Entities;

public class ProposalVersion
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public int VersionNumber { get; set; }
    public decimal Price { get; set; }
    public decimal VendorCost { get; set; }
    public decimal MarginPercentage { get; set; }
    public string ScopeOfWork { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public decimal? NotToExceedPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ChangeNotes { get; set; }
    public Proposal Proposal { get; set; } = null!;
}
