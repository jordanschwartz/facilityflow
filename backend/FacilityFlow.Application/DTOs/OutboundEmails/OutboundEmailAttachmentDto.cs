namespace FacilityFlow.Application.DTOs.OutboundEmails;

public record OutboundEmailAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize);
