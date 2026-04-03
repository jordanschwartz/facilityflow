namespace FacilityFlow.Core.Entities;

public class WorkOrderDocument
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid VendorInviteId { get; set; }
    public int Version { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public VendorInvite VendorInvite { get; set; } = null!;
}
