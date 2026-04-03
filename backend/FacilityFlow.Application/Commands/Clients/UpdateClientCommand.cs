using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Clients;

public record UpdateClientCommand(Guid Id, UpdateClientRequest Request) : IRequest<ClientDto>;

public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientDto>
{
    private readonly IRepository<Client> _clients;

    public UpdateClientCommandHandler(IRepository<Client> clients) => _clients = clients;

    public async Task<ClientDto> Handle(UpdateClientCommand command, CancellationToken cancellationToken)
    {
        var client = await _clients.Query()
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Client not found.");

        client.CompanyName = command.Request.CompanyName;
        client.ContactName = command.Request.ContactName;
        client.Email = command.Request.Email;
        client.Phone = command.Request.Phone;
        client.Address = command.Request.Address;
        client.WorkOrderPrefix = command.Request.WorkOrderPrefix;

        await _clients.SaveChangesAsync();

        return new ClientDto(
            client.Id,
            client.CompanyName,
            client.ContactName,
            client.Email,
            client.Phone,
            client.Address,
            client.WorkOrderPrefix
        );
    }
}
