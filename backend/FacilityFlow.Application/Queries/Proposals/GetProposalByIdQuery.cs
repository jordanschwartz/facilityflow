using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Application.DTOs.ServiceRequests;
using FacilityFlow.Application.DTOs.VendorInvites;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Proposals;

public record GetProposalByIdQuery(Guid Id) : IRequest<ProposalDto>;

public class GetProposalByIdQueryHandler : IRequestHandler<GetProposalByIdQuery, ProposalDto>
{
    private readonly IProposalRepository _proposals;

    public GetProposalByIdQueryHandler(IProposalRepository proposals)
        => _proposals = proposals;

    public async Task<ProposalDto> Handle(GetProposalByIdQuery request, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.GetWithFullDetailsAsync(request.Id)
            ?? throw new NotFoundException("Proposal not found.");

        return BuildProposalDto(proposal);
    }

    internal static ProposalDto BuildProposalDto(Proposal proposal)
    {
        var sr = proposal.ServiceRequest;
        var srSummary = new ServiceRequestSummaryDto(
            sr.Id,
            sr.Title,
            sr.Priority.ToString(),
            sr.Status.ToString(),
            sr.ClientId,
            sr.CreatedAt,
            sr.UpdatedAt,
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone, sr.Client.WorkOrderPrefix),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null,
            sr.WorkOrderNumber
        );

        var quoteSummary = new QuoteSummaryDto(
            proposal.Quote.Id,
            proposal.Quote.Status.ToString(),
            proposal.Quote.Price == 0 ? null : proposal.Quote.Price,
            proposal.Quote.SubmittedAt
        );

        var attachments = proposal.Attachments.Select(pa =>
            new ProposalAttachmentDto(
                pa.Id,
                pa.ProposalId,
                pa.AttachmentId,
                new AttachmentDto(pa.Attachment.Id, pa.Attachment.Url, pa.Attachment.Filename, pa.Attachment.MimeType)))
            .ToList();

        var versions = proposal.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ProposalVersionDto(
                v.Id, v.ProposalId, v.VersionNumber, v.Price,
                v.VendorCost, v.MarginPercentage, v.ScopeOfWork,
                v.Summary, v.NotToExceedPrice, v.CreatedAt, v.ChangeNotes))
            .ToList();

        var lineItems = proposal.LineItems
            .OrderBy(li => li.SortOrder)
            .Select(li => new ProposalLineItemDto(
                li.Id, li.Description, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice, li.SortOrder))
            .ToList();

        return new ProposalDto(
            proposal.Id,
            proposal.ServiceRequestId,
            proposal.QuoteId,
            proposal.Price,
            proposal.VendorCost,
            proposal.MarginPercentage,
            proposal.ScopeOfWork,
            proposal.Summary,
            proposal.SummaryGeneratedByAi,
            proposal.NotToExceedPrice,
            proposal.UseNtePricing,
            proposal.ProposedStartDate,
            proposal.EstimatedDuration,
            proposal.TermsAndConditions,
            proposal.InternalNotes,
            proposal.Status.ToString(),
            proposal.PublicToken,
            proposal.Version,
            proposal.SentAt,
            proposal.ClientResponse,
            proposal.ClientRespondedAt,
            srSummary,
            quoteSummary,
            proposal.ProposalNumber,
            lineItems,
            attachments,
            versions
        );
    }
}
