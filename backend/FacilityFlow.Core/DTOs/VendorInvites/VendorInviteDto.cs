using FacilityFlow.Core.DTOs.Common;

namespace FacilityFlow.Core.DTOs.VendorInvites;

public record VendorInviteDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid VendorId,
    string Status,
    DateTime SentAt,
    VendorSummaryDto Vendor,
    QuoteSummaryDto? Quote);
