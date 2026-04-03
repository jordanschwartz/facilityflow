using FacilityFlow.Application.DTOs.Common;

namespace FacilityFlow.Application.DTOs.VendorInvites;

public record VendorInviteDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid VendorId,
    string Status,
    DateTime SentAt,
    VendorSummaryDto Vendor,
    QuoteSummaryDto? Quote,
    string? PublicToken = null);
