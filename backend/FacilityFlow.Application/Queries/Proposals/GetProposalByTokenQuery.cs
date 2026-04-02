using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Proposals;

public record GetProposalByTokenQuery(string Token) : IRequest<ClientProposalDto>;

public class GetProposalByTokenQueryHandler : IRequestHandler<GetProposalByTokenQuery, ClientProposalDto>
{
    private readonly IProposalRepository _proposals;

    public GetProposalByTokenQueryHandler(IProposalRepository proposals)
        => _proposals = proposals;

    public async Task<ClientProposalDto> Handle(GetProposalByTokenQuery request, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.GetByTokenAsync(request.Token)
            ?? throw new NotFoundException("Proposal not found.");

        var attachments = proposal.Attachments.Select(pa =>
            new ClientProposalAttachmentDto(pa.Attachment.Id, pa.Attachment.Filename, pa.Attachment.Url))
            .ToList();

        var serviceRequestDto = new ClientProposalServiceRequestDto(
            proposal.ServiceRequest.Title,
            proposal.ServiceRequest.Location,
            proposal.ServiceRequest.Category);

        return new ClientProposalDto(
            proposal.Id,
            proposal.Price,
            proposal.ScopeOfWork,
            proposal.Summary,
            proposal.NotToExceedPrice,
            proposal.UseNtePricing,
            proposal.ProposedStartDate,
            proposal.EstimatedDuration,
            proposal.TermsAndConditions,
            proposal.Status.ToString(),
            proposal.SentAt,
            proposal.ClientResponse,
            proposal.ClientRespondedAt,
            attachments,
            serviceRequestDto);
    }
}
