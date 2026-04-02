namespace FacilityFlow.Application.DTOs.Vendors;

public record AddProspectVendorRequest(
    string CompanyName,
    string? PrimaryContactName,
    string? Email,
    string? Phone,
    string PrimaryZip,
    string? Website,
    decimal? Rating,
    int? ReviewCount,
    string? GoogleProfileUrl,
    List<string>? Trades);
