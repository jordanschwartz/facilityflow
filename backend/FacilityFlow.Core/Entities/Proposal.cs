using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class Proposal
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid QuoteId { get; set; }
    public decimal Price { get; set; }
    public string ScopeOfWork { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    public string? PublicToken { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ClientResponse { get; set; }
    public DateTime? ClientRespondedAt { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Quote Quote { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
}
