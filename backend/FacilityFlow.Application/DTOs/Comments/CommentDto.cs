using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Core.DTOs.Auth;

namespace FacilityFlow.Application.DTOs.Comments;

public record CommentDto(
    Guid Id,
    string Text,
    Guid AuthorId,
    Guid? ServiceRequestId,
    Guid? QuoteId,
    Guid? WorkOrderId,
    DateTime CreatedAt,
    UserDto Author,
    List<AttachmentDto> Attachments);
