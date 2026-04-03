using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record CreateServiceRequestCommand(CreateServiceRequestRequest Request, Guid UserId) : IRequest<ServiceRequestDto>;

public class CreateServiceRequestCommandHandler : IRequestHandler<CreateServiceRequestCommand, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IRepository<Client> _clients;
    private readonly INotificationService _notifications;

    private readonly IActivityLogger _activityLogger;

    public CreateServiceRequestCommandHandler(
        IServiceRequestRepository serviceRequests,
        IRepository<Client> clients,
        INotificationService notifications,
        IActivityLogger activityLogger)
    {
        _serviceRequests = serviceRequests;
        _clients = clients;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<ServiceRequestDto> Handle(CreateServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var client = await _clients.Query()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == req.ClientId, cancellationToken)
            ?? throw new NotFoundException("Client not found.");

        var sr = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Description = req.Description,
            Location = req.Location,
            Category = req.Category,
            Priority = req.Priority,
            Status = ServiceRequestStatus.New,
            ClientId = req.ClientId,
            CreatedById = command.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Generate work order number
        if (!string.IsNullOrWhiteSpace(client.WorkOrderPrefix))
        {
            var year = DateTime.UtcNow.ToString("yy");
            var prefix = client.WorkOrderPrefix.ToUpper();

            // Count existing service requests for this client to get the next number
            var count = await _serviceRequests.Query()
                .CountAsync(sr2 => sr2.ClientId == client.Id && sr2.WorkOrderNumber != null, cancellationToken);

            sr.WorkOrderNumber = $"{prefix}-{year}-{(count + 1):D6}";
        }

        _serviceRequests.Add(sr);
        await _serviceRequests.SaveChangesAsync();

        await _notifications.CreateAsync(client.UserId, "ServiceRequest.Created",
            $"A new service request '{sr.Title}' has been created for your account.",
            $"/service-requests/{sr.Id}");

        await _activityLogger.LogAsync(
            sr.Id, null,
            "Created work order",
            ActivityLogCategory.System, string.Empty, null);

        var result = await _serviceRequests.GetWithDetailsAsync(sr.Id);

        return new ServiceRequestDto(
            result!.Id,
            result.Title,
            result.Description,
            result.Location,
            result.Category,
            result.Priority.ToString(),
            result.Status.ToString(),
            result.ClientId,
            result.CreatedById,
            result.CreatedAt,
            result.UpdatedAt,
            new ClientSummaryDto(result.Client.Id, result.Client.CompanyName, result.Client.Phone, result.Client.WorkOrderPrefix),
            result.CreatedBy.Adapt<UserDto>(),
            result.Quotes.Count,
            result.Proposal != null,
            result.WorkOrder != null,
            result.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
            result.WorkOrderNumber,
            result.PoNumber,
            result.PoAmount,
            result.PoFileUrl,
            result.PoReceivedAt,
            result.ScheduledDate,
            result.ScheduleConfirmedAt
        );
    }
}
