using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record UpdateScheduleCommand(Guid Id, DateTime ScheduledDate) : IRequest<ServiceRequestDto>;

public class UpdateScheduleCommandHandler : IRequestHandler<UpdateScheduleCommand, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;

    public UpdateScheduleCommandHandler(IServiceRequestRepository serviceRequests)
        => _serviceRequests = serviceRequests;

    public async Task<ServiceRequestDto> Handle(UpdateScheduleCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Service request not found.");

        sr.ScheduledDate = DateTime.SpecifyKind(command.ScheduledDate, DateTimeKind.Utc);
        sr.UpdatedAt = DateTime.UtcNow;

        if (sr.Status == ServiceRequestStatus.SchedulingSiteVisit)
        {
            sr.Status = ServiceRequestStatus.ScheduleConfirmed;
            sr.ScheduleConfirmedAt = DateTime.UtcNow;
        }

        await _serviceRequests.SaveChangesAsync();

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
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone),
            sr.CreatedBy.Adapt<UserDto>(),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null,
            sr.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
            sr.PoNumber,
            sr.PoAmount,
            sr.PoFileUrl,
            sr.PoReceivedAt,
            sr.ScheduledDate,
            sr.ScheduleConfirmedAt
        );
    }
}
