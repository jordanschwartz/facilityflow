namespace FacilityFlow.Core.Interfaces.Services;

public interface IAiSummaryService
{
    Task<string> GenerateProposalSummaryAsync(string scopeOfWork, string? notes, string? jobDescription, string? additionalContext);
}
