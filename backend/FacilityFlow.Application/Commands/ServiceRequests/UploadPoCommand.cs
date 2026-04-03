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

public record UploadPoCommand(Guid Id, string PoNumber, decimal? PoAmount, Stream FileStream, string FileName, string ContentType) : IRequest<ServiceRequestDto>;

public class UploadPoCommandHandler : IRequestHandler<UploadPoCommand, ServiceRequestDto>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;

    public UploadPoCommandHandler(IServiceRequestRepository serviceRequests, IFileStorageService fileStorage, IActivityLogger activityLogger)
    {
        _serviceRequests = serviceRequests;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
    }

    public async Task<ServiceRequestDto> Handle(UploadPoCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(command.Id)
            ?? throw new NotFoundException("Service request not found.");

        if (sr.Status != ServiceRequestStatus.AwaitingPO)
            throw new InvalidOperationException("Service request is not awaiting a PO.");

        var (url, _) = await _fileStorage.SaveFileAsync($"po/{sr.Id}", command.FileStream, command.FileName, command.ContentType);

        sr.PoNumber = command.PoNumber;
        sr.PoAmount = command.PoAmount;
        sr.PoFileUrl = url;
        sr.PoReceivedAt = DateTime.UtcNow;
        sr.Status = ServiceRequestStatus.POReceived;
        sr.UpdatedAt = DateTime.UtcNow;

        await _serviceRequests.SaveChangesAsync();

        await _activityLogger.LogAsync(
            sr.Id, null,
            $"Uploaded purchase order (PO #{command.PoNumber})",
            ActivityLogCategory.Financial, string.Empty, null);

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
