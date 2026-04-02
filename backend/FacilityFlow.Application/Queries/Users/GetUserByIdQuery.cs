using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Users;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDetailDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    private readonly IRepository<User> _repo;

    public GetUserByIdQueryHandler(IRepository<User> repo) => _repo = repo;

    public async Task<UserDetailDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("User not found.");

        return ToDetailDto(user);
    }

    internal static UserDetailDto ToDetailDto(User u) => new(
        u.Id, u.FirstName, u.LastName, u.Email,
        u.Status.ToString(), u.IsAdmin, u.Role.ToString(),
        u.CreatedAt, u.LastLoginAt, u.UpdatedAt, u.PasswordChangedAt);
}
