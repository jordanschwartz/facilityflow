namespace FacilityFlow.Application.DTOs.Clients;

public record ClientDto(Guid Id, string CompanyName, string ContactName, string Email, string Phone, string Address, string? WorkOrderPrefix = null);
