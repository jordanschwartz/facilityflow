using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.Dashboard;
using FacilityFlow.Core.DTOs.ServiceRequests;
using FacilityFlow.Core.Enums;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Operator")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline()
    {
        var allSrs = await _db.ServiceRequests
            .Include(sr => sr.Client)
            .Include(sr => sr.Quotes)
            .Include(sr => sr.Proposal)
            .Include(sr => sr.WorkOrder)
            .OrderByDescending(sr => sr.UpdatedAt)
            .ToListAsync();

        var statuses = new[]
        {
            ServiceRequestStatus.New,
            ServiceRequestStatus.Sourcing,
            ServiceRequestStatus.Quoting,
            ServiceRequestStatus.PendingApproval,
            ServiceRequestStatus.Approved,
            ServiceRequestStatus.Rejected,
            ServiceRequestStatus.Completed
        };

        var columns = new Dictionary<string, PipelineColumnDto>();
        foreach (var status in statuses)
        {
            var items = allSrs.Where(sr => sr.Status == status).ToList();
            var dtos = items.Select(sr => new ServiceRequestSummaryDto(
                sr.Id,
                sr.Title,
                sr.Priority.ToString(),
                sr.Status.ToString(),
                sr.ClientId,
                sr.CreatedAt,
                sr.UpdatedAt,
                new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone),
                sr.Quotes.Count,
                sr.Proposal != null,
                sr.WorkOrder != null
            )).ToList();

            columns[status.ToString()] = new PipelineColumnDto(dtos.Count, dtos);
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var stats = new DashboardStatsDto(
            TotalOpenRequests: allSrs.Count(sr => sr.Status != ServiceRequestStatus.Completed && sr.Status != ServiceRequestStatus.Rejected),
            PendingQuotes: allSrs.Count(sr => sr.Status == ServiceRequestStatus.Quoting),
            AwaitingApproval: allSrs.Count(sr => sr.Status == ServiceRequestStatus.PendingApproval),
            CompletedThisMonth: allSrs.Count(sr => sr.Status == ServiceRequestStatus.Completed && sr.UpdatedAt >= startOfMonth)
        );

        return Ok(new PipelineResponse(columns, stats));
    }
}
