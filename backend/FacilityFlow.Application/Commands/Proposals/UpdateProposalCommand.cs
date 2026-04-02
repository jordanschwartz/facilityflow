using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Application.Queries.Proposals;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Proposals;

public record UpdateProposalCommand(Guid Id, UpdateProposalRequest Request) : IRequest<ProposalDto>;

public class UpdateProposalCommandHandler : IRequestHandler<UpdateProposalCommand, ProposalDto>
{
    private readonly IProposalRepository _proposals;
    private readonly IRepository<ProposalVersion> _versions;
    private readonly IRepository<ProposalAttachment> _proposalAttachments;

    public UpdateProposalCommandHandler(
        IProposalRepository proposals,
        IRepository<ProposalVersion> versions,
        IRepository<ProposalAttachment> proposalAttachments)
    {
        _proposals = proposals;
        _versions = versions;
        _proposalAttachments = proposalAttachments;
    }

    public async Task<ProposalDto> Handle(UpdateProposalCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var proposal = await _proposals.Query()
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Proposal not found.");

        if (proposal.Status == ProposalStatus.Sent)
        {
            CreateVersionSnapshot(proposal, req.ChangeNotes);
            proposal.Status = ProposalStatus.Draft;
        }
        else if (proposal.Status != ProposalStatus.Draft)
        {
            throw new InvalidOperationException("Only draft or sent proposals can be updated.");
        }

        if (req.MarginPercentage.HasValue)
        {
            proposal.MarginPercentage = req.MarginPercentage.Value;
            proposal.Price = proposal.VendorCost * (1 + req.MarginPercentage.Value / 100m);
        }

        if (req.ScopeOfWork != null) proposal.ScopeOfWork = req.ScopeOfWork;
        if (req.Summary != null) proposal.Summary = req.Summary;
        if (req.NotToExceedPrice.HasValue) proposal.NotToExceedPrice = req.NotToExceedPrice;
        if (req.UseNtePricing.HasValue) proposal.UseNtePricing = req.UseNtePricing.Value;
        if (req.ProposedStartDate.HasValue) proposal.ProposedStartDate = req.ProposedStartDate;
        if (req.EstimatedDuration != null) proposal.EstimatedDuration = req.EstimatedDuration;
        if (req.TermsAndConditions != null) proposal.TermsAndConditions = req.TermsAndConditions;
        if (req.InternalNotes != null) proposal.InternalNotes = req.InternalNotes;

        if (req.AttachmentIds != null)
        {
            _proposalAttachments.RemoveRange(proposal.Attachments);
            foreach (var attachmentId in req.AttachmentIds)
            {
                _proposalAttachments.Add(new ProposalAttachment
                {
                    ProposalId = proposal.Id,
                    AttachmentId = attachmentId
                });
            }
        }

        proposal.Version++;
        await _proposals.SaveChangesAsync();

        var result = await _proposals.GetWithFullDetailsAsync(command.Id);
        return GetProposalByIdQueryHandler.BuildProposalDto(result!);
    }

    private void CreateVersionSnapshot(Proposal proposal, string? changeNotes)
    {
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
            ChangeNotes = changeNotes
        });
    }
}
