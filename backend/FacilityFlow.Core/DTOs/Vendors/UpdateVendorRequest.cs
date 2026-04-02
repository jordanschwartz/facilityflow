namespace FacilityFlow.Core.DTOs.Vendors;

public record UpdateVendorRequest(string CompanyName, string Phone, List<string> Trades, List<string> ZipCodes);
