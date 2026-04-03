using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Clients;

public record CreateClientCommand(CreateClientRequest Request) : IRequest<ClientDto>;

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
{
    private readonly IRepository<Client> _clients;

    public CreateClientCommandHandler(IRepository<Client> clients)
    {
        _clients = clients;
    }

    public async Task<ClientDto> Handle(CreateClientCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var client = new Client
        {
            Id = Guid.NewGuid(),
            CompanyName = req.CompanyName,
            ContactName = req.ContactName,
            Email = req.Email,
            Phone = req.Phone,
            Address = req.Address,
            WorkOrderPrefix = req.WorkOrderPrefix
        };

        _clients.Add(client);
        await _clients.SaveChangesAsync();

        return new ClientDto(client.Id, client.CompanyName, client.ContactName, client.Email, client.Phone, client.Address, client.WorkOrderPrefix);
    }
}
