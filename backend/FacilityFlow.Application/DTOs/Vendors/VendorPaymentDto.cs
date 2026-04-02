using FacilityFlow.Core.Enums;

namespace FacilityFlow.Application.DTOs.Vendors;

public record VendorPaymentDto(
    Guid Id,
    Guid VendorId,
    Guid? WorkOrderId,
    decimal Amount,
    PaymentStatus Status,
    DateTime? PaidAt,
    string? Notes,
    DateTime CreatedAt);
