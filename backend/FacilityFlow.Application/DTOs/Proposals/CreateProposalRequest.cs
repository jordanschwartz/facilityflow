namespace FacilityFlow.Application.DTOs.Proposals;

public record CreateProposalRequest(
    Guid QuoteId,
    decimal MarginPercentage,
    decimal? Price = null,
    string? ScopeOfWork = null,
    string? Summary = null,
    decimal? NotToExceedPrice = null,
    bool UseNtePricing = false,
    DateTime? ProposedStartDate = null,
    string? EstimatedDuration = null,
    string? TermsAndConditions = null,
    string? InternalNotes = null,
    Guid[]? AttachmentIds = null,
    string? ProposalNumber = null,
    ProposalLineItemInput[]? LineItems = null);
