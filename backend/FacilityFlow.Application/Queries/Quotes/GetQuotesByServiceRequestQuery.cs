using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Quotes;

public record GetQuotesByServiceRequestQuery(Guid ServiceRequestId) : IRequest<List<QuoteDto>>;

public class GetQuotesByServiceRequestQueryHandler : IRequestHandler<GetQuotesByServiceRequestQuery, List<QuoteDto>>
{
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IQuoteRepository _quotes;

    public GetQuotesByServiceRequestQueryHandler(IRepository<ServiceRequest> serviceRequests, IQuoteRepository quotes)
    {
        _serviceRequests = serviceRequests;
        _quotes = quotes;
    }

    public async Task<List<QuoteDto>> Handle(GetQuotesByServiceRequestQuery query, CancellationToken cancellationToken)
    {
        _ = await _serviceRequests.GetByIdAsync(query.ServiceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var quotes = await _quotes.GetByServiceRequestIdAsync(query.ServiceRequestId);

        return quotes.Select(QuoteMappingHelper.MapToDto).ToList();
    }
}

internal static class QuoteMappingHelper
{
    internal static QuoteDto MapToDto(Quote q) => new(
        q.Id,
        q.ServiceRequestId,
        q.VendorId,
        q.Price,
        q.ScopeOfWork,
        q.Status.ToString(),
        q.PublicToken,
        q.SubmittedAt,
        new VendorSummaryDto(q.Vendor.Id, q.Vendor.CompanyName, q.Vendor.Trades, q.Vendor.Rating),
        q.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
        q.ProposedStartDate,
        q.EstimatedDurationValue,
        q.EstimatedDurationUnit,
        q.NotToExceedPrice,
        q.Assumptions,
        q.Exclusions,
        q.VendorAvailability,
        q.ValidUntil,
        q.LineItems.Select(li => new QuoteLineItemDto(li.Id, li.Description, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice)).ToList()
    );
}
