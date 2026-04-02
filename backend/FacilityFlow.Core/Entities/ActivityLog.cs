using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class ActivityLog
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string Action { get; set; } = string.Empty;
    public ActivityLogCategory Category { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
}
