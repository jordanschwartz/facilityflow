using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Users;

public record GetUsersQuery(string? Search, int Page, int PageSize) : IRequest<PagedResult<UserListDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListDto>>
{
    private readonly IRepository<User> _repo;

    public GetUsersQueryHandler(IRepository<User> repo) => _repo = repo;

    public async Task<PagedResult<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(u => new UserListDto(
            u.Id, u.FirstName, u.LastName, u.Email,
            u.Status.ToString(), u.Role.ToString(),
            u.CreatedAt, u.LastLoginAt)).ToList();

        return new PagedResult<UserListDto>(dtos, total, request.Page, request.PageSize);
    }
}
