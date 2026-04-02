using FacilityFlow.Core.Enums;

namespace FacilityFlow.Application.DTOs.Vendors;

public record UpdateVendorPaymentRequest(PaymentStatus Status, DateTime? PaidAt, string? Notes);
