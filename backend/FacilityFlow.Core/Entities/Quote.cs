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

    // Scheduling
    public DateTime? ProposedStartDate { get; set; }
    public int? EstimatedDurationValue { get; set; }
    public string? EstimatedDurationUnit { get; set; }

    // Financial
    public decimal? NotToExceedPrice { get; set; }

    // Risk clarity
    public string? Assumptions { get; set; }
    public string? Exclusions { get; set; }

    // Availability
    public string? VendorAvailability { get; set; }

    // Meta
    public DateTime? ValidUntil { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public Proposal? Proposal { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<QuoteLineItem> LineItems { get; set; } = new List<QuoteLineItem>();
}
