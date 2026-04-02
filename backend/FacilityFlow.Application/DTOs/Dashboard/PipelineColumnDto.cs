using FacilityFlow.Application.DTOs.ServiceRequests;

namespace FacilityFlow.Application.DTOs.Dashboard;

public record PipelineColumnDto(int Count, List<ServiceRequestSummaryDto> Items);
