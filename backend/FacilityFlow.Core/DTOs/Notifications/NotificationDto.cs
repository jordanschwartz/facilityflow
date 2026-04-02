namespace FacilityFlow.Core.DTOs.Notifications;

public record NotificationDto(Guid Id, string Type, string Message, bool Read, string? Link, DateTime CreatedAt);
