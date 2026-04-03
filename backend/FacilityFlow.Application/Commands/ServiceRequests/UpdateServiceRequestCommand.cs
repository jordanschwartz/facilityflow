using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record UpdateServiceRequestCommand(Guid Id, UpdateServiceRequestRequest Request) : IRequest<ServiceRequestDto>;

public class UpdateServiceRequestCommandHandler : IRequestHandler<UpdateServiceRequestCommand, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IActivityLogger _activityLogger;

    public UpdateServiceRequestCommandHandler(IServiceRequestRepository serviceRequests, IActivityLogger activityLogger)
    {
        _serviceRequests = serviceRequests;
        _activityLogger = activityLogger;
    }

    public async Task<ServiceRequestDto> Handle(UpdateServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Service request not found.");

        var req = command.Request;
        var changes = new List<string>();
        if (sr.Title != req.Title) changes.Add("title");
        if (sr.Description != req.Description) changes.Add("description");
        if (sr.Location != req.Location) changes.Add("location");
        if (sr.Category != req.Category) changes.Add("category");
        if (sr.Priority != req.Priority) changes.Add($"priority to {req.Priority}");

        sr.Title = req.Title;
        sr.Description = req.Description;
        sr.Location = req.Location;
        sr.Category = req.Category;
        sr.Priority = req.Priority;
        sr.UpdatedAt = DateTime.UtcNow;

        await _serviceRequests.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await _activityLogger.LogAsync(
                sr.Id, null,
                $"Updated {string.Join(", ", changes)}",
                ActivityLogCategory.System, string.Empty, null);
        }

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
