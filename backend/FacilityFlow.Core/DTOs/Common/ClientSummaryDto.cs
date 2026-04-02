namespace FacilityFlow.Core.DTOs.Common;

public record ClientSummaryDto(Guid Id, string CompanyName, string? Phone = null);
