using FacilityFlow.Core.Enums;

namespace FacilityFlow.Application.DTOs.ServiceRequests;

public record UpdateServiceRequestStatusRequest(ServiceRequestStatus Status);
