using FacilityFlow.Core.DTOs.Auth;

namespace FacilityFlow.Application.DTOs.Clients;

public record ClientDto(Guid Id, Guid UserId, string CompanyName, string Phone, string Address, UserDto User, string? WorkOrderPrefix = null);
