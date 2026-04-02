namespace FacilityFlow.Core.DTOs.Vendors;

public record VendorNoteDto(
    Guid Id,
    Guid VendorId,
    string Text,
    string? AttachmentUrl,
    string? AttachmentFilename,
    string CreatedByName,
    DateTime CreatedAt);
