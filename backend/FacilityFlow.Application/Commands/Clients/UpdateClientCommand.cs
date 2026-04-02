using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
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
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Client not found.");

        client.CompanyName = command.Request.CompanyName;
        client.Phone = command.Request.Phone;
        client.Address = command.Request.Address;

        await _clients.SaveChangesAsync();

        return new ClientDto(
            client.Id,
            client.UserId,
            client.CompanyName,
            client.Phone,
            client.Address,
            client.User.Adapt<UserDto>()
        );
    }
}
