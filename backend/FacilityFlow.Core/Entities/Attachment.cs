namespace FacilityFlow.Core.Entities;

public class Attachment
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public Guid? ServiceRequestId { get; set; }
    public Guid? QuoteId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
    public Quote? Quote { get; set; }
    public WorkOrder? WorkOrder { get; set; }
}
