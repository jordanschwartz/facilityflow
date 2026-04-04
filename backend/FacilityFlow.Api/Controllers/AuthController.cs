using FacilityFlow.Api.Extensions;
using FacilityFlow.Core.Authorization;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _authService.LoginAsync(req);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var result = await _authService.GetMeAsync(userId);
        return Ok(result);
    }

    [HttpGet("permissions")]
    [Authorize]
    public IActionResult GetPermissions()
    {
        var role = User.GetRole();
        if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            return Ok(new { permissions = Array.Empty<string>() });

        var permissions = RolePermissions.GetPermissions(userRole)
            .Select(p => p.ToString())
            .ToList();

        return Ok(new { permissions });
    }
}
