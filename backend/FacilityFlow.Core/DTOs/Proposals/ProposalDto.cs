using FacilityFlow.Core.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.VendorInvites;

namespace FacilityFlow.Core.DTOs.Proposals;

public record ProposalDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid QuoteId,
    decimal Price,
    string ScopeOfWork,
    string Status,
    string? PublicToken,
    DateTime? SentAt,
    string? ClientResponse,
    DateTime? ClientRespondedAt,
    ServiceRequestSummaryDto ServiceRequest,
    QuoteSummaryDto Quote);
