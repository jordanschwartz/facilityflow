namespace FacilityFlow.Core.DTOs.Vendors;

public record UpdateVendorRequest(
    string CompanyName,
    string PrimaryContactName,
    string Email,
    string Phone,
    string PrimaryZip,
    int ServiceRadiusMiles,
    List<string> Trades,
    List<string>? ZipCodes,
    bool IsActive,
    bool IsDnu,
    string? DnuReason);
