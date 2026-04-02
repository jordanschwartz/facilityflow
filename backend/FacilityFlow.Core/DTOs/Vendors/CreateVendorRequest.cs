namespace FacilityFlow.Core.DTOs.Vendors;

public record CreateVendorRequest(Guid UserId, string CompanyName, string Phone, List<string> Trades, List<string> ZipCodes);
