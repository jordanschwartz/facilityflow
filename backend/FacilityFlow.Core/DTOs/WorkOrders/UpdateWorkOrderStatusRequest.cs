using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.DTOs.WorkOrders;

public record UpdateWorkOrderStatusRequest(WorkOrderStatus Status, string? VendorNotes);
