namespace FacilityFlow.Application.DTOs.Common;

public record VendorSummaryDto(Guid Id, string CompanyName, List<string> Trades, decimal? Rating);
