using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Application.Queries.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Users;

public record UpdateProfileCommand(Guid UserId, UpdateProfileRequest Request) : IRequest<UserDetailDto>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDetailDto>
{
    private readonly IRepository<User> _repo;

    public UpdateProfileCommandHandler(IRepository<User> repo) => _repo = repo;

    public async Task<UserDetailDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.UserId)
            ?? throw new NotFoundException("User not found.");

        var req = command.Request;
        var emailNormalized = req.Email.ToLower().Trim();

        if (emailNormalized != user.Email &&
            await _repo.Query().AnyAsync(u => u.Email == emailNormalized && u.Id != command.UserId, cancellationToken))
            throw new InvalidOperationException("Email already registered.");

        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.Email = emailNormalized;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return GetUserByIdQueryHandler.ToDetailDto(user);
    }
}
