using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class OutboundEmail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceRequestId { get; set; }
    public string RecipientAddress { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public Guid SentById { get; set; }
    public string SentByName { get; set; } = string.Empty;
    public OutboundEmailType EmailType { get; set; }
    public string? ConversationId { get; set; }

    // Navigation
    public ServiceRequest ServiceRequest { get; set; } = null!;
    public User SentBy { get; set; } = null!;
    public List<OutboundEmailAttachment> Attachments { get; set; } = new();
}
