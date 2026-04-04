using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Application.Queries.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Users;

public record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest<UserDetailDto>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDetailDto>
{
    private readonly IRepository<User> _repo;

    public UpdateUserCommandHandler(IRepository<User> repo) => _repo = repo;

    public async Task<UserDetailDto> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.Id)
            ?? throw new NotFoundException("User not found.");

        var req = command.Request;
        var emailNormalized = req.Email.ToLower().Trim();

        if (emailNormalized != user.Email &&
            await _repo.Query().AnyAsync(u => u.Email == emailNormalized && u.Id != command.Id, cancellationToken))
            throw new InvalidOperationException("Email already registered.");

        if (!Enum.TryParse<UserStatus>(req.Status, true, out var status))
            throw new InvalidOperationException($"Invalid status: {req.Status}");

        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.Email = emailNormalized;
        user.Status = status;

        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            if (!Enum.TryParse<UserRole>(req.Role, true, out var newRole))
                throw new InvalidOperationException($"Invalid role: {req.Role}");
            user.Role = newRole;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return GetUserByIdQueryHandler.ToDetailDto(user);
    }
}
