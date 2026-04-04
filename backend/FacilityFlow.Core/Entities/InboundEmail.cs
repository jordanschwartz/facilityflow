namespace FacilityFlow.Core.Entities;

public class InboundEmail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ServiceRequestId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string MessageId { get; set; } = string.Empty;
    public string? RawHeaders { get; set; }
    public string? ConversationId { get; set; }
    public string? InReplyToMessageId { get; set; }

    // Navigation
    public ServiceRequest? ServiceRequest { get; set; }
    public List<InboundEmailAttachment> Attachments { get; set; } = new();
}
