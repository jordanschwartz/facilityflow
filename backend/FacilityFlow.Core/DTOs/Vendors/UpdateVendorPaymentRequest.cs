using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.DTOs.Vendors;

public record UpdateVendorPaymentRequest(PaymentStatus Status, DateTime? PaidAt, string? Notes);
