namespace FacilityFlow.Application.DTOs.OutboundEmails;

public record OutboundEmailDetailDto(
    Guid Id,
    Guid ServiceRequestId,
    string RecipientAddress,
    string? RecipientName,
    string Subject,
    string BodyHtml,
    DateTime SentAt,
    string SentByName,
    string EmailType,
    string? ConversationId,
    List<OutboundEmailAttachmentDto> Attachments);
