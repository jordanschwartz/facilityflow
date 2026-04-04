namespace FacilityFlow.Application.DTOs.InboundEmails;

public record InboundEmailDto(
    Guid Id,
    Guid? ServiceRequestId,
    string FromAddress,
    string? FromName,
    string Subject,
    string? BodyPreview,
    DateTime ReceivedAt,
    int AttachmentCount);
