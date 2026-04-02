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
    List<AttachmentDto> Attachments);
