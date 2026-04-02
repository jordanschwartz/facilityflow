namespace FacilityFlow.Core.DTOs.Vendors;

public record CreateVendorRequest(
    Guid? UserId,
    string CompanyName,
    string PrimaryContactName,
    string Email,
    string? Phone,
    string PrimaryZip,
    int ServiceRadiusMiles,
    List<string>? Trades,
    List<string>? ZipCodes,
    bool IsActive = true,
    bool IsDnu = false,
    string? DnuReason = null);
