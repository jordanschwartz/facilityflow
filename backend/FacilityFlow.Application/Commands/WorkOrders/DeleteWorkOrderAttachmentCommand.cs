using FacilityFlow.Core.Entities;
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

    public DeleteWorkOrderAttachmentCommandHandler(
        IRepository<WorkOrder> workOrders, IRepository<Attachment> attachments, IFileStorageService fileStorage)
    {
        _workOrders = workOrders;
        _attachments = attachments;
        _fileStorage = fileStorage;
    }

    public async Task<Unit> Handle(DeleteWorkOrderAttachmentCommand command, CancellationToken cancellationToken)
    {
        if (!await _workOrders.ExistsAsync(command.WorkOrderId))
            throw new NotFoundException("Work order not found.");

        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(a => a.Id == command.AttachmentId && a.WorkOrderId == command.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException("Attachment not found.");

        _fileStorage.DeleteFile(attachment.Url);
        _attachments.Remove(attachment);
        await _attachments.SaveChangesAsync();

        return Unit.Value;
    }
}
