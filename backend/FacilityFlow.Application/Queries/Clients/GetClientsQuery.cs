using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Clients;

public record GetClientsQuery(string? Search, int Page, int PageSize) : IRequest<PagedResult<ClientDto>>;

public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, PagedResult<ClientDto>>
{
    private readonly IRepository<Client> _clients;

    public GetClientsQueryHandler(IRepository<Client> clients) => _clients = clients;

    public async Task<PagedResult<ClientDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var query = _clients.Query().Include(c => c.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c => c.CompanyName.ToLower().Contains(search)
                                  || c.User.FirstName.ToLower().Contains(search)
                                  || c.User.LastName.ToLower().Contains(search)
                                  || c.User.Email.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.CompanyName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(c => new ClientDto(
            c.Id,
            c.UserId,
            c.CompanyName,
            c.Phone,
            c.Address,
            c.User.Adapt<UserDto>()
        )).ToList();

        return new PagedResult<ClientDto>(dtos, total, request.Page, request.PageSize);
    }
}
