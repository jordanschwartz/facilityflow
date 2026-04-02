namespace FacilityFlow.Application.DTOs.Comments;

public record CreateCommentRequest(string Text, Guid? ServiceRequestId, Guid? QuoteId, Guid? WorkOrderId);
