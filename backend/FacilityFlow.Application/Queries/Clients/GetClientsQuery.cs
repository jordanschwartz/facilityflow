using FacilityFlow.Application.DTOs.Clients;
using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
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
        var query = _clients.Query().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c => c.CompanyName.ToLower().Contains(search)
                                  || c.ContactName.ToLower().Contains(search)
                                  || c.Email.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.CompanyName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(c => new ClientDto(
            c.Id,
            c.CompanyName,
            c.ContactName,
            c.Email,
            c.Phone,
            c.Address,
            c.WorkOrderPrefix
        )).ToList();

        return new PagedResult<ClientDto>(dtos, total, request.Page, request.PageSize);
    }
}
