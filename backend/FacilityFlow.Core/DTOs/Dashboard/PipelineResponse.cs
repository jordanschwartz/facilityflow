namespace FacilityFlow.Core.DTOs.Dashboard;

public record PipelineResponse(Dictionary<string, PipelineColumnDto> Columns, DashboardStatsDto Stats);
