namespace FacilityFlow.Application.DTOs.Proposals;

public record ProposalLineItemInput(string Description, decimal Quantity, decimal UnitPrice, int SortOrder = 0);
