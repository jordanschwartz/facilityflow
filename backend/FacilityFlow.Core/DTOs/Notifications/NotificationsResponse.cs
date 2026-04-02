namespace FacilityFlow.Core.DTOs.Notifications;

public record NotificationsResponse(List<NotificationDto> Items, int UnreadCount, int TotalCount);
