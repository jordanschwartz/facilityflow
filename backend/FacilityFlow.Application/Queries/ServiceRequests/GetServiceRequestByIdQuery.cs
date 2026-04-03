using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Queries.ServiceRequests;

public record GetServiceRequestByIdQuery(Guid Id) : IRequest<ServiceRequestDto>;

public class GetServiceRequestByIdQueryHandler : IRequestHandler<GetServiceRequestByIdQuery, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;

    public GetServiceRequestByIdQueryHandler(IServiceRequestRepository serviceRequests)
        => _serviceRequests = serviceRequests;

    public async Task<ServiceRequestDto> Handle(GetServiceRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(request.Id)
            ?? throw new NotFoundException("Service request not found.");

        return new ServiceRequestDto(
            sr.Id,
            sr.Title,
            sr.Description,
            sr.Location,
            sr.Category,
            sr.Priority.ToString(),
            sr.Status.ToString(),
            sr.ClientId,
            sr.CreatedById,
            sr.CreatedAt,
            sr.UpdatedAt,
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone, sr.Client.WorkOrderPrefix),
            sr.CreatedBy.Adapt<UserDto>(),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null,
            sr.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
            sr.WorkOrderNumber,
            sr.PoNumber,
            sr.PoAmount,
            sr.PoFileUrl,
            sr.PoReceivedAt,
            sr.ScheduledDate,
            sr.ScheduleConfirmedAt
        );
    }
}
