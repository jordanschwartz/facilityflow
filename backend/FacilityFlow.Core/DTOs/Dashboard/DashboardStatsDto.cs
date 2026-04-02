namespace FacilityFlow.Core.DTOs.Dashboard;

public record DashboardStatsDto(int TotalOpenRequests, int PendingQuotes, int AwaitingApproval, int CompletedThisMonth);
