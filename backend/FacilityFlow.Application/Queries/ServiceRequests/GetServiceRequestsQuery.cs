using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.ServiceRequests;

public record GetServiceRequestsQuery(
    string? Status,
    string? Priority,
    Guid? ClientId,
    string? Search,
    int Page,
    int PageSize) : IRequest<PagedResult<ServiceRequestSummaryDto>>;

public class GetServiceRequestsQueryHandler : IRequestHandler<GetServiceRequestsQuery, PagedResult<ServiceRequestSummaryDto>>
{
    private readonly IServiceRequestRepository _serviceRequests;

    public GetServiceRequestsQueryHandler(IServiceRequestRepository serviceRequests)
        => _serviceRequests = serviceRequests;

    public async Task<PagedResult<ServiceRequestSummaryDto>> Handle(GetServiceRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _serviceRequests.Query()
            .Include(sr => sr.Client).ThenInclude(c => c.User)
            .Include(sr => sr.CreatedBy)
            .Include(sr => sr.Quotes)
            .Include(sr => sr.Proposal)
            .Include(sr => sr.WorkOrder)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ServiceRequestStatus>(request.Status, true, out var parsedStatus))
            query = query.Where(sr => sr.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(request.Priority) && Enum.TryParse<Priority>(request.Priority, true, out var parsedPriority))
            query = query.Where(sr => sr.Priority == parsedPriority);

        if (request.ClientId.HasValue)
            query = query.Where(sr => sr.ClientId == request.ClientId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(sr => sr.Title.ToLower().Contains(request.Search.ToLower())
                                   || sr.Description.ToLower().Contains(request.Search.ToLower()));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(sr => sr.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

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

        return new PagedResult<ServiceRequestSummaryDto>(dtos, total, request.Page, request.PageSize);
    }
}
