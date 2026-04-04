using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Application.Queries.Users;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Users;

public record CreateUserCommand(CreateUserRequest Request) : IRequest<UserDetailDto>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDetailDto>
{
    private readonly IRepository<User> _repo;

    public CreateUserCommandHandler(IRepository<User> repo) => _repo = repo;

    public async Task<UserDetailDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var emailNormalized = req.Email.ToLower().Trim();

        if (await _repo.Query().AnyAsync(u => u.Email == emailNormalized, cancellationToken))
            throw new InvalidOperationException("Email already registered.");

        if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
            throw new InvalidOperationException($"Invalid role: {req.Role}");

        var password = req.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            password = GenerateTemporaryPassword();
        }

        if (password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = emailNormalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repo.Add(user);
        await _repo.SaveChangesAsync();

        return GetUserByIdQueryHandler.ToDetailDto(user);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
        var random = new Random();
        return new string(Enumerable.Range(0, 12).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
