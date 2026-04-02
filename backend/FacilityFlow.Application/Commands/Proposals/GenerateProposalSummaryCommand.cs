using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;

namespace FacilityFlow.Application.Commands.Proposals;

public record GenerateProposalSummaryCommand(Guid Id, GenerateSummaryRequest Request) : IRequest<GenerateSummaryResponse>;

public class GenerateProposalSummaryCommandHandler : IRequestHandler<GenerateProposalSummaryCommand, GenerateSummaryResponse>
{
    private readonly IProposalRepository _proposals;
    private readonly IAiSummaryService _aiSummaryService;

    public GenerateProposalSummaryCommandHandler(IProposalRepository proposals, IAiSummaryService aiSummaryService)
    {
        _proposals = proposals;
        _aiSummaryService = aiSummaryService;
    }

    public async Task<GenerateSummaryResponse> Handle(GenerateProposalSummaryCommand command, CancellationToken cancellationToken)
    {
        if (!await _proposals.ExistsAsync(command.Id))
            throw new NotFoundException("Proposal not found.");

        var req = command.Request;
        var summary = await _aiSummaryService.GenerateProposalSummaryAsync(
            req.ScopeOfWork, req.Notes, req.JobDescription, req.AdditionalContext);

        return new GenerateSummaryResponse(summary);
    }
}
