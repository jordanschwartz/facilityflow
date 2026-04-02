namespace FacilityFlow.Core.DTOs.Proposals;

public record UpdateProposalRequest(
    decimal? MarginPercentage = null,
    string? ScopeOfWork = null,
    string? Summary = null,
    decimal? NotToExceedPrice = null,
    bool? UseNtePricing = null,
    DateTime? ProposedStartDate = null,
    string? EstimatedDuration = null,
    string? TermsAndConditions = null,
    string? InternalNotes = null,
    Guid[]? AttachmentIds = null,
    string? ChangeNotes = null);
