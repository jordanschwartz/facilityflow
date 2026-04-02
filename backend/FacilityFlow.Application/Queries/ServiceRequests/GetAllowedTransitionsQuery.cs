using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.StateMachines;
using MediatR;

namespace FacilityFlow.Application.Queries.ServiceRequests;

public record GetAllowedTransitionsQuery(Guid Id) : IRequest<List<string>>;

public class GetAllowedTransitionsQueryHandler : IRequestHandler<GetAllowedTransitionsQuery, List<string>>
{
    private readonly IServiceRequestRepository _serviceRequests;

    public GetAllowedTransitionsQueryHandler(IServiceRequestRepository serviceRequests)
        => _serviceRequests = serviceRequests;

    public async Task<List<string>> Handle(GetAllowedTransitionsQuery request, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Service request not found.");

        return ServiceRequestStateMachine.GetAllowedTransitions(sr.Status)
            .Select(s => s.ToString())
            .ToList();
    }
}
