namespace FacilityFlow.Core.DTOs.Vendors;

public record VendorSourcingResultDto(
    Guid VendorId,
    string CompanyName,
    string PrimaryContactName,
    string Email,
    string PrimaryZip,
    int ServiceRadiusMiles,
    List<string> Trades,
    bool IsDnu,
    string? DnuReason,
    int CompletedJobCount,
    DateTime? LastUsedDate);
