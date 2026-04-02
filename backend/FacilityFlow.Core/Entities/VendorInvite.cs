using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class VendorInvite
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid VendorId { get; set; }
    public VendorInviteStatus Status { get; set; } = VendorInviteStatus.Invited;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
}
