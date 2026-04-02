using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.Clients;

public record CreateClientCommand(CreateClientRequest Request) : IRequest<ClientDto>;

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
{
    private readonly IRepository<Client> _clients;
    private readonly IRepository<User> _users;

    public CreateClientCommandHandler(IRepository<Client> clients, IRepository<User> users)
    {
        _clients = clients;
        _users = users;
    }

    public async Task<ClientDto> Handle(CreateClientCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var user = await _users.GetByIdAsync(req.UserId)
            ?? throw new NotFoundException("User not found.");

        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            CompanyName = req.CompanyName,
            Phone = req.Phone,
            Address = req.Address
        };

        _clients.Add(client);
        await _clients.SaveChangesAsync();

        return new ClientDto(client.Id, client.UserId, client.CompanyName, client.Phone, client.Address, user.Adapt<UserDto>());
    }
}
