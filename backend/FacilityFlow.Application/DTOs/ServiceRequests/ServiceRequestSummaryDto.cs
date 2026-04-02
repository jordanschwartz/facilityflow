using FacilityFlow.Application.DTOs.Common;

namespace FacilityFlow.Application.DTOs.ServiceRequests;

public record ServiceRequestSummaryDto(
    Guid Id,
    string Title,
    string Priority,
    string Status,
    Guid ClientId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ClientSummaryDto Client,
    int QuoteCount,
    bool HasProposal,
    bool HasWorkOrder);
