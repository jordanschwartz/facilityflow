namespace FacilityFlow.Core.DTOs.Proposals;

public record RespondToProposalRequest(string? Token, string Decision, string? ClientResponse);
