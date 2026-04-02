using FacilityFlow.Core.DTOs.Common;

namespace FacilityFlow.Core.DTOs.Proposals;

public record ClientProposalDto(
    Guid Id,
    decimal Price,
    string ScopeOfWork,
    string? Summary,
    decimal? NotToExceedPrice,
    bool UseNtePricing,
    DateTime? ProposedStartDate,
    string? EstimatedDuration,
    string? TermsAndConditions,
    string Status,
    List<AttachmentDto> Attachments,
    ClientProposalServiceRequestDto ServiceRequest);

public record ClientProposalServiceRequestDto(
    string Title,
    string Location,
    string Category);
