using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class ServiceRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.New;
    public Guid ClientId { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Client Client { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<VendorInvite> VendorInvites { get; set; } = new List<VendorInvite>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public Proposal? Proposal { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // PO tracking
    public string? PoNumber { get; set; }
    public decimal? PoAmount { get; set; }
    public string? PoFileUrl { get; set; }
    public DateTime? PoReceivedAt { get; set; }

    // Scheduling
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ScheduleConfirmedAt { get; set; }
}
