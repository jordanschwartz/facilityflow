namespace FacilityFlow.Application.DTOs.Proposals;

public record UpdateProposalRequest(
    decimal? MarginPercentage = null,
    decimal? Price = null,
    string? ScopeOfWork = null,
    string? Summary = null,
    decimal? NotToExceedPrice = null,
    bool? UseNtePricing = null,
    DateTime? ProposedStartDate = null,
    string? EstimatedDuration = null,
    string? TermsAndConditions = null,
    string? InternalNotes = null,
    Guid[]? AttachmentIds = null,
    string? ChangeNotes = null,
    string? ProposalNumber = null,
    ProposalLineItemInput[]? LineItems = null);
