namespace FacilityFlow.Core.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public Guid? ServiceRequestId { get; set; }
    public Guid? QuoteId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Author { get; set; } = null!;
    public ServiceRequest? ServiceRequest { get; set; }
    public Quote? Quote { get; set; }
    public WorkOrder? WorkOrder { get; set; }
}
