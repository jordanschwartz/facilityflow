using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record UpdateServiceRequestCommand(Guid Id, UpdateServiceRequestRequest Request) : IRequest<ServiceRequestDto>;

public class UpdateServiceRequestCommandHandler : IRequestHandler<UpdateServiceRequestCommand, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;

    public UpdateServiceRequestCommandHandler(IServiceRequestRepository serviceRequests)
        => _serviceRequests = serviceRequests;

    public async Task<ServiceRequestDto> Handle(UpdateServiceRequestCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Service request not found.");

        var req = command.Request;
        sr.Title = req.Title;
        sr.Description = req.Description;
        sr.Location = req.Location;
        sr.Category = req.Category;
        sr.Priority = req.Priority;
        sr.UpdatedAt = DateTime.UtcNow;

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
