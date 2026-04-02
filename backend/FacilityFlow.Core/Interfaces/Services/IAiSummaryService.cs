namespace FacilityFlow.Core.Interfaces.Services;

public class ProposalSummaryContext
{
    public string ScopeOfWork { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? JobDescription { get; init; }
    public string? AdditionalContext { get; init; }
    public decimal? NotToExceedPrice { get; init; }
    public DateTime? ProposedStartDate { get; init; }
    public int? EstimatedDurationValue { get; init; }
    public string? EstimatedDurationUnit { get; init; }
    public string? Assumptions { get; init; }
    public string? Exclusions { get; init; }
    public List<string> AttachmentFilenames { get; init; } = [];
}

public interface IAiSummaryService
{
    Task<string> GenerateProposalSummaryAsync(ProposalSummaryContext context);
}
