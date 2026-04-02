using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Quotes;

public record GetQuoteByTokenQuery(string Token) : IRequest<object>;

public class GetQuoteByTokenQueryHandler : IRequestHandler<GetQuoteByTokenQuery, object>
{
    private readonly IQuoteRepository _quotes;

    public GetQuoteByTokenQueryHandler(IQuoteRepository quotes)
        => _quotes = quotes;

    public async Task<object> Handle(GetQuoteByTokenQuery query, CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetByTokenWithDetailsAsync(query.Token)
            ?? throw new NotFoundException("Quote not found.");

        return new
        {
            serviceRequest = new
            {
                title = quote.ServiceRequest.Title,
                location = quote.ServiceRequest.Location,
                category = quote.ServiceRequest.Category,
                description = quote.ServiceRequest.Description,
            },
            quote = QuoteMappingHelper.MapToDto(quote)
        };
    }
}
