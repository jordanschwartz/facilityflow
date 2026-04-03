namespace FacilityFlow.Application.DTOs.WorkOrders;

public record WorkOrderViewDto(
    string WorkOrderNumber,
    string Title,
    string Description,
    string Category,
    string Priority,
    string ClientName,
    string ServiceLocation,
    DateTime RequestedDate,
    DateTime? ScheduledDate,
    string ContactName,
    string ContactEmail,
    string? QuoteToken,
    string VendorName,
    string Status);
