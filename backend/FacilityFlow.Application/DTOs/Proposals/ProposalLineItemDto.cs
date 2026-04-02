namespace FacilityFlow.Application.DTOs.Proposals;

public record ProposalLineItemDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal Total, int SortOrder);
