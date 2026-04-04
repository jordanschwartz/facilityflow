namespace FacilityFlow.Core.Entities;

public class InboundEmailAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InboundEmailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }

    // Navigation
    public InboundEmail InboundEmail { get; set; } = null!;
}
