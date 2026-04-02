using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.WorkOrders;

public record DeleteWorkOrderAttachmentCommand(Guid WorkOrderId, Guid AttachmentId) : IRequest<Unit>;

public class DeleteWorkOrderAttachmentCommandHandler : IRequestHandler<DeleteWorkOrderAttachmentCommand, Unit>
{
    private readonly IRepository<WorkOrder> _workOrders;
    private readonly IRepository<Attachment> _attachments;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;

    public DeleteWorkOrderAttachmentCommandHandler(
        IRepository<WorkOrder> workOrders, IRepository<Attachment> attachments,
        IFileStorageService fileStorage, IActivityLogger activityLogger)
    {
        _workOrders = workOrders;
        _attachments = attachments;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
    }

    public async Task<Unit> Handle(DeleteWorkOrderAttachmentCommand command, CancellationToken cancellationToken)
    {
        var workOrder = await _workOrders.GetByIdAsync(command.WorkOrderId)
            ?? throw new NotFoundException("Work order not found.");

        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(a => a.Id == command.AttachmentId && a.WorkOrderId == command.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException("Attachment not found.");

        var fileName = attachment.Filename;

        _fileStorage.DeleteFile(attachment.Url);
        _attachments.Remove(attachment);
        await _attachments.SaveChangesAsync();

        await _activityLogger.LogAsync(
            workOrder.ServiceRequestId, workOrder.Id,
            $"Deleted attachment: {fileName}",
            ActivityLogCategory.FileUpload, string.Empty, null);

        return Unit.Value;
    }
}
