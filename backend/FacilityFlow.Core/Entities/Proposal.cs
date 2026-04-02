using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class Proposal
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid QuoteId { get; set; }
    public decimal Price { get; set; }
    public decimal VendorCost { get; set; }
    public decimal MarginPercentage { get; set; }
    public decimal? NotToExceedPrice { get; set; }
    public bool UseNtePricing { get; set; }
    public string ScopeOfWork { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public bool SummaryGeneratedByAi { get; set; }
    public DateTime? ProposedStartDate { get; set; }
    public string? EstimatedDuration { get; set; }
    public string? TermsAndConditions { get; set; }
    public int Version { get; set; } = 1;
    public string? InternalNotes { get; set; }
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    public string? PublicToken { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ClientResponse { get; set; }
    public DateTime? ClientRespondedAt { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Quote Quote { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
    public ICollection<ProposalAttachment> Attachments { get; set; } = new List<ProposalAttachment>();
    public ICollection<ProposalVersion> Versions { get; set; } = new List<ProposalVersion>();
}
