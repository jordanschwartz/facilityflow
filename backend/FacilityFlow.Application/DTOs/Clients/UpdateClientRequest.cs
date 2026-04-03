namespace FacilityFlow.Application.DTOs.Clients;

public record UpdateClientRequest(string CompanyName, string Phone, string Address, string? WorkOrderPrefix = null);
