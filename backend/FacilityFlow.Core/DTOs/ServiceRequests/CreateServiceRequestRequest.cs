using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.DTOs.ServiceRequests;

public record CreateServiceRequestRequest(string Title, string Description, string Location, string Category, Priority Priority, Guid ClientId);
