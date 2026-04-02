using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower().Trim()))
            throw new InvalidOperationException("Email already registered.");

        if (req.Role == UserRole.Operator)
            throw new ForbiddenException("Cannot self-register as Operator.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = req.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Name = req.Name,
            Role = req.Role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(token, user.Adapt<UserDto>());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower().Trim())
            ?? throw new NotFoundException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new NotFoundException("Invalid credentials.");

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(token, user.Adapt<UserDto>());
    }

    public async Task<UserDto> GetMeAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found.");
        return user.Adapt<UserDto>();
    }
}
