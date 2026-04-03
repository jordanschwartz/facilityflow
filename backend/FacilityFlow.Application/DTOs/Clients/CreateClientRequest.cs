namespace FacilityFlow.Application.DTOs.Clients;

public record CreateClientRequest(string CompanyName, string ContactName, string Email, string Phone, string Address, string? WorkOrderPrefix = null);
