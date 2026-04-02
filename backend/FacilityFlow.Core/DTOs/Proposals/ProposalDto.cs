using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.VendorInvites;

namespace FacilityFlow.Core.DTOs.Proposals;

public record ProposalDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid QuoteId,
    decimal Price,
    decimal VendorCost,
    decimal MarginPercentage,
    string ScopeOfWork,
    string? Summary,
    bool SummaryGeneratedByAi,
    decimal? NotToExceedPrice,
    bool UseNtePricing,
    DateTime? ProposedStartDate,
    string? EstimatedDuration,
    string? TermsAndConditions,
    string? InternalNotes,
    string Status,
    string? PublicToken,
    int Version,
    DateTime? SentAt,
    string? ClientResponse,
    DateTime? ClientRespondedAt,
    ServiceRequestSummaryDto ServiceRequest,
    QuoteSummaryDto Quote,
    List<ProposalAttachmentDto> Attachments,
    List<ProposalVersionDto> Versions);

public record ProposalAttachmentDto(
    int Id,
    Guid ProposalId,
    Guid AttachmentId,
    AttachmentDto Attachment);
