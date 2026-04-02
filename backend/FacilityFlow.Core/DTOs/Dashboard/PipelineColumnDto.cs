using FacilityFlow.Core.DTOs.ServiceRequests;

namespace FacilityFlow.Core.DTOs.Dashboard;

public record PipelineColumnDto(int Count, List<ServiceRequestSummaryDto> Items);
