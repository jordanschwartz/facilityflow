using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.ServiceRequests;

namespace FacilityFlow.Core.DTOs.WorkOrders;

public record WorkOrderDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid ProposalId,
    Guid VendorId,
    string Status,
    string? VendorNotes,
    DateTime? CompletedAt,
    ServiceRequestSummaryDto ServiceRequest,
    VendorSummaryDto Vendor,
    List<AttachmentDto> Attachments);
