namespace FacilityFlow.Application.DTOs.InboundEmails;

public record InboundEmailAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize);
