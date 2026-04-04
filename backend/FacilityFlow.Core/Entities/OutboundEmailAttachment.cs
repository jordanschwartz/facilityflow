namespace FacilityFlow.Core.Entities;

public class OutboundEmailAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OutboundEmailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }

    // Navigation
    public OutboundEmail OutboundEmail { get; set; } = null!;
}
