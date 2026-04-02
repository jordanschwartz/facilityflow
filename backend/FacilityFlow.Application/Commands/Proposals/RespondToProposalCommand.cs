using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Application.Queries.Proposals;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Proposals;

public record RespondToProposalCommand(Guid Id, RespondToProposalRequest Request) : IRequest<ProposalDto>;

public class RespondToProposalCommandHandler : IRequestHandler<RespondToProposalCommand, ProposalDto>
{
    private readonly IProposalRepository _proposals;
    private readonly IRepository<WorkOrder> _workOrders;
    private readonly IRepository<Vendor> _vendors;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IActivityLogger _activityLogger;

    public RespondToProposalCommandHandler(
        IProposalRepository proposals,
        IRepository<WorkOrder> workOrders,
        IRepository<Vendor> vendors,
        IUserRepository users,
        INotificationService notifications,
        IActivityLogger activityLogger)
    {
        _proposals = proposals;
        _workOrders = workOrders;
        _vendors = vendors;
        _users = users;
        _notifications = notifications;
        _activityLogger = activityLogger;
    }

    public async Task<ProposalDto> Handle(RespondToProposalCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        Proposal? proposal;

        if (!string.IsNullOrWhiteSpace(req.Token))
        {
            proposal = await _proposals.Query()
                .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
                .Include(p => p.Quote)
                .FirstOrDefaultAsync(p => p.PublicToken == req.Token, cancellationToken)
                ?? throw new NotFoundException("Proposal not found.");
        }
        else
        {
            proposal = await _proposals.Query()
                .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
                .Include(p => p.Quote)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
                ?? throw new NotFoundException("Proposal not found.");
        }

        if (proposal.Status != ProposalStatus.Sent)
            throw new InvalidOperationException("Proposal is not in a state that can be responded to.");

        var decision = req.Decision.ToLower();
        if (decision != "approved" && decision != "rejected")
            throw new InvalidOperationException("Decision must be 'approved' or 'rejected'.");

        proposal.Status = decision == "approved" ? ProposalStatus.Approved : ProposalStatus.Rejected;
        proposal.ClientResponse = req.ClientResponse;
        proposal.ClientRespondedAt = DateTime.UtcNow;

        if (proposal.Status == ProposalStatus.Approved)
        {
            proposal.ServiceRequest.Status = ServiceRequestStatus.AwaitingPO;
            proposal.ServiceRequest.UpdatedAt = DateTime.UtcNow;

            var workOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = proposal.ServiceRequestId,
                ProposalId = proposal.Id,
                VendorId = proposal.Quote.VendorId,
                Status = WorkOrderStatus.Assigned
            };
            _workOrders.Add(workOrder);

            var vendor = await _vendors.GetByIdAsync(proposal.Quote.VendorId);
            if (vendor != null && vendor.UserId.HasValue)
            {
                await _notifications.CreateAsync(vendor.UserId.Value, "WorkOrder.Assigned",
                    $"You have been assigned a new work order for: {proposal.ServiceRequest.Title}",
                    $"/work-orders/{workOrder.Id}");
            }

            var operators = await _users.GetByRoleAsync(UserRole.Operator);
            foreach (var op in operators)
            {
                await _notifications.CreateAsync(op.Id, "Proposal.Approved",
                    $"Proposal approved for: {proposal.ServiceRequest.Title}",
                    $"/service-requests/{proposal.ServiceRequestId}");
            }
        }

        await _proposals.SaveChangesAsync();

        var action = proposal.Status == ProposalStatus.Approved
            ? "Client approved proposal"
            : "Client rejected proposal";
        await _activityLogger.LogAsync(
            proposal.ServiceRequestId, null,
            action,
            ActivityLogCategory.StatusChange, "Client", null);

        var result = await _proposals.GetWithFullDetailsAsync(proposal.Id);
        return GetProposalByIdQueryHandler.BuildProposalDto(result!);
    }
}
