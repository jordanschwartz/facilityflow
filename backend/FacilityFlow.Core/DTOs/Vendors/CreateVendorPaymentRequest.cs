using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.DTOs.Vendors;

public record CreateVendorPaymentRequest(
    Guid? WorkOrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTime? PaidAt,
    string? Notes);
