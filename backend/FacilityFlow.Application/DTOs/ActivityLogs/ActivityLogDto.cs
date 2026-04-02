namespace FacilityFlow.Application.DTOs.ActivityLogs;

public record ActivityLogDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid? WorkOrderId,
    string Action,
    string Category,
    string ActorName,
    Guid? ActorId,
    DateTime CreatedAt);
