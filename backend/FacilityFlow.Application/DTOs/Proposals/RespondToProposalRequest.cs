namespace FacilityFlow.Application.DTOs.Proposals;

public record RespondToProposalRequest(string? Token, string Decision, string? ClientResponse);
