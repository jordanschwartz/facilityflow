namespace FacilityFlow.Core.DTOs.Common;

public record VendorSummaryDto(Guid Id, string CompanyName, List<string> Trades, decimal? Rating);
