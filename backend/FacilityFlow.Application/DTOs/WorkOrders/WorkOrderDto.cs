using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;

namespace FacilityFlow.Application.DTOs.WorkOrders;

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
