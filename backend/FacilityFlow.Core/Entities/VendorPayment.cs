using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class VendorPayment
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Vendor Vendor { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
}
