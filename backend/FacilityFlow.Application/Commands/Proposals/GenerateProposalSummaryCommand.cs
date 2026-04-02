using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Proposals;

public record GenerateProposalSummaryCommand(Guid Id, GenerateSummaryRequest Request) : IRequest<GenerateSummaryResponse>;

public class GenerateProposalSummaryCommandHandler : IRequestHandler<GenerateProposalSummaryCommand, GenerateSummaryResponse>
{
    private readonly IProposalRepository _proposals;
    private readonly IQuoteRepository _quotes;
    private readonly IAiSummaryService _aiSummaryService;

    public GenerateProposalSummaryCommandHandler(
        IProposalRepository proposals, IQuoteRepository quotes, IAiSummaryService aiSummaryService)
    {
        _proposals = proposals;
        _quotes = quotes;
        _aiSummaryService = aiSummaryService;
    }

    public async Task<GenerateSummaryResponse> Handle(GenerateProposalSummaryCommand command, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.Query()
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Proposal not found.");

        var quote = await _quotes.Query()
            .Include(q => q.Attachments)
            .FirstOrDefaultAsync(q => q.Id == proposal.QuoteId, cancellationToken);

        var req = command.Request;
        var context = new ProposalSummaryContext
        {
            ScopeOfWork = req.ScopeOfWork,
            Notes = req.Notes,
            JobDescription = req.JobDescription,
            AdditionalContext = req.AdditionalContext,
            NotToExceedPrice = quote?.NotToExceedPrice,
            ProposedStartDate = quote?.ProposedStartDate,
            EstimatedDurationValue = quote?.EstimatedDurationValue,
            EstimatedDurationUnit = quote?.EstimatedDurationUnit,
            Assumptions = quote?.Assumptions,
            Exclusions = quote?.Exclusions,
            AttachmentFilenames = quote?.Attachments.Select(a => a.Filename).ToList() ?? []
        };

        var summary = await _aiSummaryService.GenerateProposalSummaryAsync(context);
        return new GenerateSummaryResponse(summary);
    }
}
