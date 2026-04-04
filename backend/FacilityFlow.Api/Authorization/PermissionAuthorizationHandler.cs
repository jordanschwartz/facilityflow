using FacilityFlow.Core.Enums;
using Microsoft.AspNetCore.Authorization;

namespace FacilityFlow.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }
    public PermissionRequirement(Permission permission) => Permission = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionsClaim = context.User.FindFirst("permissions")?.Value;
        if (string.IsNullOrEmpty(permissionsClaim))
            return Task.CompletedTask;

        var permissions = permissionsClaim
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToHashSet();

        if (permissions.Contains(Permission.FullAccess.ToString()) ||
            permissions.Contains(requirement.Permission.ToString()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
