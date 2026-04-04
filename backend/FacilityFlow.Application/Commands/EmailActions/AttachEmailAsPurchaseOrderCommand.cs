using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.EmailActions;

public record AttachEmailAsPurchaseOrderCommand(Guid InboundEmailId, Guid AttachmentId) : IRequest;

public class AttachEmailAsPurchaseOrderCommandHandler : IRequestHandler<AttachEmailAsPurchaseOrderCommand>
{
    private readonly IRepository<InboundEmail> _inboundEmails;
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;

    public AttachEmailAsPurchaseOrderCommandHandler(
        IRepository<InboundEmail> inboundEmails,
        IRepository<ServiceRequest> serviceRequests,
        IFileStorageService fileStorage,
        IActivityLogger activityLogger)
    {
        _inboundEmails = inboundEmails;
        _serviceRequests = serviceRequests;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
    }

    public async Task Handle(AttachEmailAsPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var email = await _inboundEmails.Query()
            .Include(e => e.Attachments)
            .Include(e => e.ServiceRequest)
            .FirstOrDefaultAsync(e => e.Id == command.InboundEmailId, cancellationToken)
            ?? throw new NotFoundException("Inbound email not found.");

        if (email.ServiceRequestId is null)
            throw new InvalidOperationException("Email is not linked to a service request.");

        var attachment = email.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId)
            ?? throw new NotFoundException("Attachment not found.");

        var sr = email.ServiceRequest
            ?? throw new NotFoundException("Service request not found.");

        var poPath = $"uploads/po/{sr.Id}/{attachment.FileName}";

        sr.PoFileUrl = poPath;
        sr.PoReceivedAt = DateTime.UtcNow;
        sr.UpdatedAt = DateTime.UtcNow;

        await _serviceRequests.SaveChangesAsync();

        await _activityLogger.LogAsync(
            sr.Id, null,
            $"Attached {attachment.FileName} as Purchase Order from email",
            ActivityLogCategory.Financial, string.Empty, null);
    }
}
