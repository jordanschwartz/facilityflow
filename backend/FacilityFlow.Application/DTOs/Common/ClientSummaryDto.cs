namespace FacilityFlow.Application.DTOs.Common;

public record ClientSummaryDto(Guid Id, string CompanyName, string? Phone = null, string? WorkOrderPrefix = null);
