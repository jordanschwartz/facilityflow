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
    DateTime? SentAt,
    string? ClientResponse,
    DateTime? ClientRespondedAt,
    List<ClientProposalAttachmentDto> Attachments,
    ClientProposalServiceRequestDto ServiceRequest);

public record ClientProposalAttachmentDto(
    Guid Id,
    string FileName,
    string FilePath);

public record ClientProposalServiceRequestDto(
    string Title,
    string Location,
    string Category);
