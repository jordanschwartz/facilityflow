using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Application.DTOs.Common;

namespace FacilityFlow.Application.DTOs.ServiceRequests;

public record ServiceRequestDto(
    Guid Id,
    string Title,
    string Description,
    string Location,
    string Category,
    string Priority,
    string Status,
    Guid ClientId,
    Guid CreatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ClientSummaryDto Client,
    UserDto CreatedBy,
    int QuoteCount,
    bool HasProposal,
    bool HasWorkOrder,
    List<AttachmentDto> Attachments);
