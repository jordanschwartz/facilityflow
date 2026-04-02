using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;

namespace FacilityFlow.Application.DTOs.WorkOrders;

public record WorkOrderSummaryDto(
    Guid Id,
    Guid ServiceRequestId,
    string Status,
    DateTime? CompletedAt,
    ServiceRequestSummaryDto ServiceRequest,
    VendorSummaryDto Vendor);
