using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Dashboard;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Dashboard;

public record GetPipelineQuery : IRequest<PipelineResponse>;

public class GetPipelineQueryHandler : IRequestHandler<GetPipelineQuery, PipelineResponse>
{
    private readonly IRepository<ServiceRequest> _serviceRequests;

    public GetPipelineQueryHandler(IRepository<ServiceRequest> serviceRequests) => _serviceRequests = serviceRequests;

    public async Task<PipelineResponse> Handle(GetPipelineQuery request, CancellationToken cancellationToken)
    {
        var allSrs = await _serviceRequests.Query()
            .Include(sr => sr.Client)
            .Include(sr => sr.Quotes)
            .Include(sr => sr.Proposal)
            .Include(sr => sr.WorkOrder)
            .OrderByDescending(sr => sr.UpdatedAt)
            .ToListAsync(cancellationToken);

        var statuses = new[]
        {
            ServiceRequestStatus.New,
            ServiceRequestStatus.Qualifying,
            ServiceRequestStatus.Sourcing,
            ServiceRequestStatus.SchedulingSiteVisit,
            ServiceRequestStatus.ScheduleConfirmed,
            ServiceRequestStatus.PendingQuotes,
            ServiceRequestStatus.ProposalReady,
            ServiceRequestStatus.PendingApproval,
            ServiceRequestStatus.AwaitingPO,
            ServiceRequestStatus.POReceived,
            ServiceRequestStatus.JobInProgress,
            ServiceRequestStatus.JobCompleted,
            ServiceRequestStatus.Verification,
            ServiceRequestStatus.InvoiceSent,
            ServiceRequestStatus.InvoicePaid,
            ServiceRequestStatus.Closed,
            ServiceRequestStatus.Cancelled
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
            TotalOpenRequests: allSrs.Count(sr => sr.Status != ServiceRequestStatus.Closed && sr.Status != ServiceRequestStatus.Cancelled),
            PendingQuotes: allSrs.Count(sr => sr.Status == ServiceRequestStatus.PendingQuotes),
            AwaitingApproval: allSrs.Count(sr => sr.Status == ServiceRequestStatus.PendingApproval),
            JobsInProgress: allSrs.Count(sr => sr.Status == ServiceRequestStatus.JobInProgress),
            CompletedThisMonth: allSrs.Count(sr => sr.Status == ServiceRequestStatus.JobCompleted && sr.UpdatedAt >= startOfMonth)
        );

        return new PipelineResponse(columns, stats);
    }
}
