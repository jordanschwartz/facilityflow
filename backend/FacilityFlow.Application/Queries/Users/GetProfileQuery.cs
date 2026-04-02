using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Queries.Users;

public record GetProfileQuery(Guid UserId) : IRequest<UserDetailDto>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserDetailDto>
{
    private readonly IRepository<User> _repo;

    public GetProfileQueryHandler(IRepository<User> repo) => _repo = repo;

    public async Task<UserDetailDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        return GetUserByIdQueryHandler.ToDetailDto(user);
    }
}
