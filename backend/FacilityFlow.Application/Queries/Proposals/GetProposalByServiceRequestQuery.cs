using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Proposals;

public record GetProposalByServiceRequestQuery(Guid ServiceRequestId) : IRequest<ProposalDto?>;

public class GetProposalByServiceRequestQueryHandler : IRequestHandler<GetProposalByServiceRequestQuery, ProposalDto?>
{
    private readonly IProposalRepository _proposals;

    public GetProposalByServiceRequestQueryHandler(IProposalRepository proposals)
        => _proposals = proposals;

    public async Task<ProposalDto?> Handle(GetProposalByServiceRequestQuery request, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.GetByServiceRequestIdAsync(request.ServiceRequestId);
        if (proposal == null) return null;

        var full = await _proposals.GetWithFullDetailsAsync(proposal.Id);
        return GetProposalByIdQueryHandler.BuildProposalDto(full!);
    }
}
