using FacilityFlow.Core.DTOs.Auth;

namespace FacilityFlow.Application.DTOs.Vendors;

public record VendorDto(
    Guid Id,
    Guid? UserId,
    string CompanyName,
    string PrimaryContactName,
    string Email,
    string Phone,
    string PrimaryZip,
    int ServiceRadiusMiles,
    List<string> Trades,
    List<string> ZipCodes,
    decimal? Rating,
    bool IsActive,
    bool IsDnu,
    string? DnuReason,
    string Status,
    string? Website,
    int? ReviewCount,
    string? GoogleProfileUrl,
    UserDto? User);
