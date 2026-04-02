namespace FacilityFlow.Application.DTOs.Proposals;

public record ProposalVersionDto(
    int Id,
    Guid ProposalId,
    int VersionNumber,
    decimal Price,
    decimal VendorCost,
    decimal MarginPercentage,
    string ScopeOfWork,
    string? Summary,
    decimal? NotToExceedPrice,
    DateTime CreatedAt,
    string? ChangeNotes);
