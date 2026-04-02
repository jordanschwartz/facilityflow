using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class WorkOrder
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid ProposalId { get; set; }
    public Guid VendorId { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Assigned;
    public string? VendorNotes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Proposal Proposal { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
