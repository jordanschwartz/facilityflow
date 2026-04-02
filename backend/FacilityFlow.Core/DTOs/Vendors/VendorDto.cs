using FacilityFlow.Core.DTOs.Auth;

namespace FacilityFlow.Core.DTOs.Vendors;

public record VendorDto(Guid Id, Guid UserId, string CompanyName, string Phone, List<string> Trades, List<string> ZipCodes, decimal? Rating, UserDto User);
