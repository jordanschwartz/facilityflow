using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.WorkOrders;

public record UpdateWorkOrderStatusCommand(Guid Id, UpdateWorkOrderStatusRequest Request) : IRequest<WorkOrderDto>;

public class UpdateWorkOrderStatusCommandHandler : IRequestHandler<UpdateWorkOrderStatusCommand, WorkOrderDto>
{
    private readonly IRepository<WorkOrder> _repo;

    public UpdateWorkOrderStatusCommandHandler(IRepository<WorkOrder> repo) => _repo = repo;

    public async Task<WorkOrderDto> Handle(UpdateWorkOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var wo = await _repo.Query()
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Proposal)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(w => w.Vendor)
            .Include(w => w.Attachments)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Work order not found.");

        wo.Status = request.Request.Status;

        if (!string.IsNullOrWhiteSpace(request.Request.VendorNotes))
            wo.VendorNotes = request.Request.VendorNotes;

        if (request.Request.Status == WorkOrderStatus.Completed)
            wo.CompletedAt = DateTime.UtcNow;

        // Closing the work order completes the service request
        if (request.Request.Status == WorkOrderStatus.Closed)
        {
            wo.ServiceRequest.Status = ServiceRequestStatus.JobCompleted;
            wo.ServiceRequest.UpdatedAt = DateTime.UtcNow;
        }

        await _repo.SaveChangesAsync();
        return MapToDetail(wo);
    }

    private static ServiceRequestSummaryDto MapSrToSummary(ServiceRequest sr) =>
        new(
            sr.Id,
            sr.Title,
            sr.Priority.ToString(),
            sr.Status.ToString(),
            sr.ClientId,
            sr.CreatedAt,
            sr.UpdatedAt,
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null
        );

    private static WorkOrderDto MapToDetail(WorkOrder wo) =>
        new(
            wo.Id,
            wo.ServiceRequestId,
            wo.ProposalId,
            wo.VendorId,
            wo.Status.ToString(),
            wo.VendorNotes,
            wo.CompletedAt,
            MapSrToSummary(wo.ServiceRequest),
            new VendorSummaryDto(wo.Vendor.Id, wo.Vendor.CompanyName, wo.Vendor.Trades, wo.Vendor.Rating),
            wo.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList()
        );
}
