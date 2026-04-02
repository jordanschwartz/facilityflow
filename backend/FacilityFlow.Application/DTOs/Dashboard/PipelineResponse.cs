namespace FacilityFlow.Application.DTOs.Dashboard;

public record PipelineResponse(Dictionary<string, PipelineColumnDto> Columns, DashboardStatsDto Stats);
