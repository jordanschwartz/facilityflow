namespace FacilityFlow.Application.DTOs.EmailConversations;

public record EmailConversationDto(
    string ConversationId,
    string Subject,
    DateTime LatestEmailAt,
    int EmailCount,
    List<EmailThreadItemDto> Emails);

public record EmailThreadItemDto(
    Guid Id,
    string Type,
    string FromAddress,
    string? FromName,
    string? ToAddress,
    string? ToName,
    string Subject,
    string? BodyPreview,
    DateTime Timestamp,
    int AttachmentCount);
