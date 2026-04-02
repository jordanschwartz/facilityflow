using FacilityFlow.Core.Enums;

namespace FacilityFlow.Application.DTOs.WorkOrders;

public record UpdateWorkOrderStatusRequest(WorkOrderStatus Status, string? VendorNotes);
