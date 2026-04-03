namespace FacilityFlow.Application.DTOs.Clients;

public record UpdateClientRequest(string CompanyName, string ContactName, string Email, string Phone, string Address, string? WorkOrderPrefix = null);
