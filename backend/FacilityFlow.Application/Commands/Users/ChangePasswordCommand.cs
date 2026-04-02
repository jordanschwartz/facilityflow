using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Users;

public record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request) : IRequest<Unit>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IRepository<User> _repo;

    public ChangePasswordCommandHandler(IRepository<User> repo) => _repo = repo;

    public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.UserId)
            ?? throw new NotFoundException("User not found.");

        var req = command.Request;

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            throw new ForbiddenException("Current password is incorrect.");

        if (req.NewPassword != req.ConfirmPassword)
            throw new InvalidOperationException("New password and confirmation do not match.");

        if (req.NewPassword.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return Unit.Value;
    }
}
