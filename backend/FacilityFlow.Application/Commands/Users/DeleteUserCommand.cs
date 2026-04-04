using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Application.Queries.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Users;

public record DeleteUserCommand(Guid Id) : IRequest<UserDetailDto>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, UserDetailDto>
{
    private readonly IRepository<User> _repo;

    public DeleteUserCommandHandler(IRepository<User> repo) => _repo = repo;

    public async Task<UserDetailDto> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.Id)
            ?? throw new NotFoundException("User not found.");

        user.Status = UserStatus.Inactive;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return GetUserByIdQueryHandler.ToDetailDto(user);
    }
}
