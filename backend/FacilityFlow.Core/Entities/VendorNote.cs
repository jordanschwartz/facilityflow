namespace FacilityFlow.Core.Entities;

public class VendorNote
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFilename { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Vendor Vendor { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
