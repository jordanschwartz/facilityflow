using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Application.Queries.Proposals;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Proposals;

public record CreateProposalCommand(Guid ServiceRequestId, CreateProposalRequest Request) : IRequest<ProposalDto>;

public class CreateProposalCommandHandler : IRequestHandler<CreateProposalCommand, ProposalDto>
{
    private const string DefaultTermsAndConditions =
        "1. Payment Terms: Payment is due within 30 days of work completion.\n" +
        "2. Warranty: All work is warranted for 90 days from completion.\n" +
        "3. Scope: This proposal covers only the work described above. Additional work will require a separate proposal.\n" +
        "4. Scheduling: Proposed dates are estimates and subject to change based on site conditions and availability.\n" +
        "5. Access: Client agrees to provide reasonable access to the work area during scheduled hours.\n" +
        "6. Cancellation: Cancellation after approval may be subject to a cancellation fee for materials already procured.";

    private readonly IProposalRepository _proposals;
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<ProposalAttachment> _proposalAttachments;

    public CreateProposalCommandHandler(
        IProposalRepository proposals,
        IRepository<ServiceRequest> serviceRequests,
        IQuoteRepository quotes,
        IRepository<ProposalAttachment> proposalAttachments)
    {
        _proposals = proposals;
        _serviceRequests = serviceRequests;
        _quotes = quotes;
        _proposalAttachments = proposalAttachments;
    }

    public async Task<ProposalDto> Handle(CreateProposalCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var sr = await _serviceRequests.Query()
            .Include(s => s.Proposal)
            .Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == command.ServiceRequestId, cancellationToken)
            ?? throw new NotFoundException("Service request not found.");

        if (sr.Proposal != null)
            throw new InvalidOperationException("A proposal already exists for this service request.");

        var quote = await _quotes.Query()
            .Include(q => q.Attachments)
            .FirstOrDefaultAsync(q => q.Id == req.QuoteId, cancellationToken)
            ?? throw new NotFoundException("Quote not found.");

        var vendorCost = quote.Price;
        var price = vendorCost * (1 + req.MarginPercentage / 100m);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = command.ServiceRequestId,
            QuoteId = req.QuoteId,
            VendorCost = vendorCost,
            MarginPercentage = req.MarginPercentage,
            Price = price,
            ScopeOfWork = req.ScopeOfWork ?? quote.ScopeOfWork,
            Summary = req.Summary,
            NotToExceedPrice = req.NotToExceedPrice,
            UseNtePricing = req.UseNtePricing,
            ProposedStartDate = req.ProposedStartDate,
            EstimatedDuration = req.EstimatedDuration,
            TermsAndConditions = req.TermsAndConditions ?? DefaultTermsAndConditions,
            InternalNotes = req.InternalNotes,
            Status = ProposalStatus.Draft,
            Version = 1,
            PublicToken = "pr-" + Guid.NewGuid().ToString("N")
        };

        _proposals.Add(proposal);

        var attachmentIds = req.AttachmentIds != null && req.AttachmentIds.Length > 0
            ? req.AttachmentIds
            : quote.Attachments.Select(a => a.Id).ToArray();

        foreach (var attachmentId in attachmentIds)
        {
            _proposalAttachments.Add(new ProposalAttachment
            {
                ProposalId = proposal.Id,
                AttachmentId = attachmentId
            });
        }

        await _proposals.SaveChangesAsync();

        var result = await _proposals.GetWithFullDetailsAsync(proposal.Id);
        return GetProposalByIdQueryHandler.BuildProposalDto(result!);
    }
}
