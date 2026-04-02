using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;

namespace FacilityFlow.Application.Commands.WorkOrders;

public record UploadWorkOrderAttachmentCommand(Guid WorkOrderId, Stream FileStream, string FileName, string ContentType) : IRequest<AttachmentDto>;

public class UploadWorkOrderAttachmentCommandHandler : IRequestHandler<UploadWorkOrderAttachmentCommand, AttachmentDto>
{
    private readonly IRepository<WorkOrder> _workOrders;
    private readonly IRepository<Attachment> _attachments;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;

    public UploadWorkOrderAttachmentCommandHandler(
        IRepository<WorkOrder> workOrders, IRepository<Attachment> attachments,
        IFileStorageService fileStorage, IActivityLogger activityLogger)
    {
        _workOrders = workOrders;
        _attachments = attachments;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
    }

    public async Task<AttachmentDto> Handle(UploadWorkOrderAttachmentCommand command, CancellationToken cancellationToken)
    {
        var workOrder = await _workOrders.GetByIdAsync(command.WorkOrderId)
            ?? throw new NotFoundException("Work order not found.");

        if (!_fileStorage.AllowedMimeTypes.Contains(command.ContentType))
            throw new InvalidOperationException("File type not allowed. Accepted: images, videos, PDF.");

        var (url, _) = await _fileStorage.SaveFileAsync(
            $"work-orders/{workOrder.Id}", command.FileStream, command.FileName, command.ContentType);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            Filename = command.FileName,
            MimeType = command.ContentType,
            Url = url
        };

        _attachments.Add(attachment);
        await _attachments.SaveChangesAsync();

        await _activityLogger.LogAsync(
            workOrder.ServiceRequestId, workOrder.Id,
            $"Uploaded attachment: {command.FileName}",
            ActivityLogCategory.FileUpload, string.Empty, null);

        return new AttachmentDto(attachment.Id, attachment.Url, attachment.Filename, attachment.MimeType);
    }
}
