using FacilityFlow.Core.DTOs.Common;

namespace FacilityFlow.Core.DTOs.Quotes;

public record QuoteDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid VendorId,
    decimal Price,
    string ScopeOfWork,
    string Status,
    string? PublicToken,
    DateTime? SubmittedAt,
    VendorSummaryDto Vendor,
    List<AttachmentDto> Attachments,
    DateTime? ProposedStartDate,
    int? EstimatedDurationValue,
    string? EstimatedDurationUnit,
    decimal? NotToExceedPrice,
    string? Assumptions,
    string? Exclusions,
    string? VendorAvailability,
    DateTime? ValidUntil,
    List<QuoteLineItemDto> LineItems
);
