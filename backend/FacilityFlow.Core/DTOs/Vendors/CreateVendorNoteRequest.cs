namespace FacilityFlow.Core.DTOs.Vendors;

public record CreateVendorNoteRequest(string Text, string? AttachmentUrl, string? AttachmentFilename);
