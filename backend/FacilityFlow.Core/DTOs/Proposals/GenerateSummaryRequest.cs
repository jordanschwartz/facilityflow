namespace FacilityFlow.Core.DTOs.Proposals;

public record GenerateSummaryRequest(
    string ScopeOfWork,
    string? Notes = null,
    string? JobDescription = null,
    string? AdditionalContext = null);
