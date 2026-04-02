using FacilityFlow.Core.DTOs.Auth;

namespace FacilityFlow.Core.DTOs.Clients;

public record ClientDto(Guid Id, Guid UserId, string CompanyName, string Phone, string Address, UserDto User);
