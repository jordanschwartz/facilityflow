using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.ServiceRequests;

namespace FacilityFlow.Core.DTOs.WorkOrders;

public record WorkOrderSummaryDto(
    Guid Id,
    Guid ServiceRequestId,
    string Status,
    DateTime? CompletedAt,
    ServiceRequestSummaryDto ServiceRequest,
    VendorSummaryDto Vendor);
