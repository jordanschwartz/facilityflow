namespace FacilityFlow.Core.DTOs.Clients;

public record CreateClientRequest(Guid UserId, string CompanyName, string Phone, string Address);
