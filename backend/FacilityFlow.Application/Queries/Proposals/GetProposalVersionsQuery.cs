using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Proposals;

public record GetProposalVersionsQuery(Guid ProposalId) : IRequest<List<ProposalVersionDto>>;

public class GetProposalVersionsQueryHandler : IRequestHandler<GetProposalVersionsQuery, List<ProposalVersionDto>>
{
    private readonly IProposalRepository _proposals;
    private readonly IRepository<ProposalVersion> _versions;

    public GetProposalVersionsQueryHandler(IProposalRepository proposals, IRepository<ProposalVersion> versions)
    {
        _proposals = proposals;
        _versions = versions;
    }

    public async Task<List<ProposalVersionDto>> Handle(GetProposalVersionsQuery request, CancellationToken cancellationToken)
    {
        if (!await _proposals.ExistsAsync(request.ProposalId))
            throw new NotFoundException("Proposal not found.");

        return await _versions.Query()
            .Where(v => v.ProposalId == request.ProposalId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ProposalVersionDto(
                v.Id, v.ProposalId, v.VersionNumber, v.Price,
                v.VendorCost, v.MarginPercentage, v.ScopeOfWork,
                v.Summary, v.NotToExceedPrice, v.CreatedAt, v.ChangeNotes))
            .ToListAsync(cancellationToken);
    }
}
