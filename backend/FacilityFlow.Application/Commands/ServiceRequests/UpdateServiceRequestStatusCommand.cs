using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Core.StateMachines;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record UpdateServiceRequestStatusCommand(Guid Id, UpdateServiceRequestStatusRequest Request) : IRequest<ServiceRequestDto>;

public class UpdateServiceRequestStatusCommandHandler : IRequestHandler<UpdateServiceRequestStatusCommand, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IActivityLogger _activityLogger;

    public UpdateServiceRequestStatusCommandHandler(IServiceRequestRepository serviceRequests, IActivityLogger activityLogger)
    {
        _serviceRequests = serviceRequests;
        _activityLogger = activityLogger;
    }

    public async Task<ServiceRequestDto> Handle(UpdateServiceRequestStatusCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Service request not found.");

        if (!ServiceRequestStateMachine.CanTransition(sr.Status, command.Request.Status))
            throw new InvalidTransitionException(sr.Status.ToString(), command.Request.Status.ToString());

        var oldStatus = sr.Status;
        sr.Status = command.Request.Status;
        sr.UpdatedAt = DateTime.UtcNow;
        await _serviceRequests.SaveChangesAsync();

        await _activityLogger.LogAsync(
            sr.Id, null,
            $"Changed status from {oldStatus} → {command.Request.Status}",
            ActivityLogCategory.StatusChange, string.Empty, null);

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
