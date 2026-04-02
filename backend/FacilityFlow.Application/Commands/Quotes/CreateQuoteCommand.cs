using FacilityFlow.Application.DTOs.Quotes;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Quotes;

public record CreateQuoteCommand(Guid ServiceRequestId, SubmitQuoteRequest Request) : IRequest<object>;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, object>
{
    private readonly IRepository<ServiceRequest> _serviceRequests;
    private readonly IQuoteRepository _quotes;

    public CreateQuoteCommandHandler(IRepository<ServiceRequest> serviceRequests, IQuoteRepository quotes)
    {
        _serviceRequests = serviceRequests;
        _quotes = quotes;
    }

    public async Task<object> Handle(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        _ = await _serviceRequests.GetByIdAsync(command.ServiceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = command.ServiceRequestId,
            VendorId = Guid.Empty,
            Price = command.Request.Price,
            ScopeOfWork = command.Request.ScopeOfWork,
            Status = QuoteStatus.Submitted,
            PublicToken = "qt-" + Guid.NewGuid().ToString("N"),
            SubmittedAt = DateTime.UtcNow
        };

        _quotes.Add(quote);
        await _quotes.SaveChangesAsync();

        return new { quote.Id };
    }
}
