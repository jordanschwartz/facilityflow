using FacilityFlow.Application.DTOs.ActivityLogs;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.ActivityLogs;

public record GetActivityLogsQuery(Guid ServiceRequestId, Guid? WorkOrderId, string? Category) : IRequest<List<ActivityLogDto>>;

public class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, List<ActivityLogDto>>
{
    private readonly IRepository<ActivityLog> _repo;

    public GetActivityLogsQueryHandler(IRepository<ActivityLog> repo) => _repo = repo;

    public async Task<List<ActivityLogDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query()
            .Where(a => a.ServiceRequestId == request.ServiceRequestId);

        if (request.WorkOrderId.HasValue)
            query = query.Where(a => a.WorkOrderId == request.WorkOrderId.Value);

        if (!string.IsNullOrWhiteSpace(request.Category) && Enum.TryParse<ActivityLogCategory>(request.Category, true, out var category))
            query = query.Where(a => a.Category == category);

        var logs = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);

        return logs.Select(a => new ActivityLogDto(
            a.Id,
            a.ServiceRequestId,
            a.WorkOrderId,
            a.Action,
            a.Category.ToString(),
            a.ActorName,
            a.ActorId,
            a.CreatedAt
        )).ToList();
    }
}
