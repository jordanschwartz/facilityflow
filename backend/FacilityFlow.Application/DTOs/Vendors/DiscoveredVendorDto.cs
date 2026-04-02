namespace FacilityFlow.Application.DTOs.Vendors;

public record DiscoveredVendorDto(
    string BusinessName,
    string Address,
    string? Phone,
    string? Website,
    decimal? Rating,
    int? ReviewCount,
    string? GoogleProfileUrl,
    Guid? ExistingVendorId);
