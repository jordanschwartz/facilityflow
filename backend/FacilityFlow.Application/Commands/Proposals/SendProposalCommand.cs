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

public record SendProposalCommand(Guid Id) : IRequest<ProposalDto>;

public class SendProposalCommandHandler : IRequestHandler<SendProposalCommand, ProposalDto>
{
    private readonly IProposalRepository _proposals;
    private readonly IRepository<ProposalVersion> _versions;
    private readonly INotificationService _notifications;

    public SendProposalCommandHandler(
        IProposalRepository proposals,
        IRepository<ProposalVersion> versions,
        INotificationService notifications)
    {
        _proposals = proposals;
        _versions = versions;
        _notifications = notifications;
    }

    public async Task<ProposalDto> Handle(SendProposalCommand command, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.Query()
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Proposal not found.");

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException("Only draft proposals can be sent.");

        _versions.Add(new ProposalVersion
        {
            ProposalId = proposal.Id,
            VersionNumber = proposal.Version,
            Price = proposal.Price,
            VendorCost = proposal.VendorCost,
            MarginPercentage = proposal.MarginPercentage,
            ScopeOfWork = proposal.ScopeOfWork,
            Summary = proposal.Summary,
            NotToExceedPrice = proposal.NotToExceedPrice,
            CreatedAt = DateTime.UtcNow,
            ChangeNotes = "Sent to client"
        });

        proposal.Status = ProposalStatus.Sent;
        proposal.SentAt = DateTime.UtcNow;
        await _proposals.SaveChangesAsync();

        await _notifications.CreateAsync(
            proposal.ServiceRequest.Client.UserId,
            "Proposal.Sent",
            $"A proposal has been sent for your service request: {proposal.ServiceRequest.Title}",
            $"/proposals/view/{proposal.PublicToken}");

        var result = await _proposals.GetWithFullDetailsAsync(command.Id);
        return GetProposalByIdQueryHandler.BuildProposalDto(result!);
    }
}
