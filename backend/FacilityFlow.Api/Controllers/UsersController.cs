using FacilityFlow.Api.Authorization;
using FacilityFlow.Api.Extensions;
using FacilityFlow.Application.Commands.Users;
using FacilityFlow.Application.DTOs.Users;
using FacilityFlow.Application.Queries.Users;
using FacilityFlow.Core.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    // ── Profile endpoints (any authenticated user) ────────────────────────────

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new GetProfileQuery(userId));
        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new UpdateProfileCommand(userId, req));
        return Ok(result);
    }

    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = User.GetUserId();
        await _mediator.Send(new ChangePasswordCommand(userId, req));
        return NoContent();
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission(Permission.ManageUsers)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetUsersQuery(search, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permission.ManageUsers)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var result = await _mediator.Send(new CreateUserCommand(req));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permission.ManageUsers)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permission.ManageUsers)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
    {
        var result = await _mediator.Send(new UpdateUserCommand(id, req));
        return Ok(result);
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permission.ManageUsers)]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var result = await _mediator.Send(new ResetUserPasswordCommand(id));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permission.ManageUsers)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var currentUserId = User.GetUserId();
        if (id == currentUserId)
            return BadRequest(new { error = "Cannot delete your own account." });

        var result = await _mediator.Send(new DeleteUserCommand(id));
        return Ok(result);
    }
}
