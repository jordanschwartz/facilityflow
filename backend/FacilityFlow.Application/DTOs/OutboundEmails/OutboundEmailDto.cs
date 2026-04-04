namespace FacilityFlow.Application.DTOs.OutboundEmails;

public record OutboundEmailDto(
    Guid Id,
    Guid ServiceRequestId,
    string RecipientAddress,
    string? RecipientName,
    string Subject,
    string? BodyPreview,
    DateTime SentAt,
    string SentByName,
    string EmailType,
    int AttachmentCount);
