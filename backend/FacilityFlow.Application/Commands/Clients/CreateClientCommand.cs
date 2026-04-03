using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Mapster;
using MediatR;

namespace FacilityFlow.Application.Commands.Clients;

public record CreateClientCommand(CreateClientRequest Request) : IRequest<ClientDto>;

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
{
    private readonly IRepository<Client> _clients;
    private readonly IAuthService _authService;

    public CreateClientCommandHandler(IRepository<Client> clients, IAuthService authService)
    {
        _clients = clients;
        _authService = authService;
    }

    public async Task<ClientDto> Handle(CreateClientCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        // Auto-create user account with a generated password
        var password = $"Client{Guid.NewGuid():N}"[..16];
        var nameParts = req.ContactName.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        var authResult = await _authService.RegisterAsync(
            new RegisterRequest(req.Email, password, firstName, lastName, UserRole.Client));

        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = authResult.User.Id,
            CompanyName = req.CompanyName,
            Phone = req.Phone,
            Address = req.Address,
            WorkOrderPrefix = req.WorkOrderPrefix
        };

        _clients.Add(client);
        await _clients.SaveChangesAsync();

        return new ClientDto(client.Id, client.UserId, client.CompanyName, client.Phone, client.Address, authResult.User.Adapt<UserDto>(), client.WorkOrderPrefix);
    }
}
