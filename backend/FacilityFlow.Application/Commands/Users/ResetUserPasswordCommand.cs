using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Users;

public record ResetUserPasswordCommand(Guid UserId) : IRequest<ResetPasswordResponse>;

public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, ResetPasswordResponse>
{
    private readonly IRepository<User> _repo;

    public ResetUserPasswordCommandHandler(IRepository<User> repo) => _repo = repo;

    public async Task<ResetPasswordResponse> Handle(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _repo.GetByIdAsync(command.UserId)
            ?? throw new NotFoundException("User not found.");

        var tempPassword = GenerateTemporaryPassword();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        return new ResetPasswordResponse(tempPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
        var random = new Random();
        return new string(Enumerable.Range(0, 12).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
