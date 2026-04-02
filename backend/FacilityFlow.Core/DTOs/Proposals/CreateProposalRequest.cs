namespace FacilityFlow.Core.DTOs.Proposals;

public record CreateProposalRequest(Guid QuoteId, decimal Price, string ScopeOfWork);
