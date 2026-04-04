namespace FacilityFlow.Application.DTOs.InboundEmails;

public record InboundEmailDetailDto(
    Guid Id,
    Guid? ServiceRequestId,
    string FromAddress,
    string? FromName,
    string Subject,
    string? BodyText,
    string? BodyHtml,
    DateTime ReceivedAt,
    string MessageId,
    List<InboundEmailAttachmentDto> Attachments);
