using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Clients;

public record GetClientByIdQuery(Guid Id) : IRequest<ClientDto>;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDto>
{
    private readonly IRepository<Client> _clients;

    public GetClientByIdQueryHandler(IRepository<Client> clients) => _clients = clients;

    public async Task<ClientDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _clients.Query()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Client not found.");

        return new ClientDto(
            client.Id,
            client.UserId,
            client.CompanyName,
            client.Phone,
            client.Address,
            client.User.Adapt<UserDto>(),
            client.WorkOrderPrefix
        );
    }
}
