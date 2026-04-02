using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class Quote
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid VendorId { get; set; }
    public decimal Price { get; set; }
    public string ScopeOfWork { get; set; } = string.Empty;
    public QuoteStatus Status { get; set; } = QuoteStatus.Requested;
    public string? PublicToken { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public Proposal? Proposal { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
