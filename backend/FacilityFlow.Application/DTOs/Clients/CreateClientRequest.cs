namespace FacilityFlow.Application.DTOs.Clients;

public record CreateClientRequest(string CompanyName, string Phone, string Address, string ContactName, string Email, string? WorkOrderPrefix = null);
